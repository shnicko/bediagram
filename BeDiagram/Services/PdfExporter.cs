using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace BeDiagram.Services;

/// <summary>
/// Turns the SVG produced by <see cref="Export.SvgDiagramWriter"/> into a vector PDF, with no third-party libraries.
/// It understands the SVG subset BeDiagram writes (and the Lucide icons): g/rect/path/circle/ellipse/line/
/// polyline/polygon/text/tspan, transforms, markers, rectangular clip paths, opacity, dashes and the
/// standard PDF fonts (Helvetica / Times / Courier). Rendering from the SVG keeps one geometry for all exports.
/// </summary>
public static class PdfExporter
{
    const double PtPerPx = 0.75, PtPerMm = 72 / 25.4;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    static string F(double v) => Math.Abs(v) < 1e-9 ? "0" : v.ToString("0.####", Inv);

    /// <summary>One page PDF, sized from the SVG's width/height (mm or px).</summary>
    public static byte[] FromSvg(string svg, string title)
    {
        XElement root = XDocument.Parse(svg).Root ?? throw new InvalidOperationException("Empty SVG");
        List<double> vb = Nums(Attr(root, "viewBox") ?? "");
        double vbX = 0, vbY = 0, vbW, vbH;
        double wPt = Length(Attr(root, "width"), out double wPx), hPt = Length(Attr(root, "height"), out double hPx);
        if (vb.Count == 4) { vbX = vb[0]; vbY = vb[1]; vbW = vb[2]; vbH = vb[3]; }
        else { vbW = wPx; vbH = hPx; }
        if (wPt <= 0) wPt = vbW * PtPerPx;
        if (hPt <= 0) hPt = vbH * PtPerPx;

        Renderer r = new Renderer();
        foreach (XElement el in root.Descendants())
        {
            string? id = Attr(el, "id");
            if (id is null) continue;
            if (el.Name.LocalName == "marker") r.Markers[id] = el;
            else if (el.Name.LocalName == "clipPath") r.Clips[id] = el;
        }
        // SVG is y-down from the viewBox origin; PDF is y-up in points.
        double sx = wPt / vbW, sy = hPt / vbH;
        r.Out.Append($"{F(sx)} 0 0 {F(-sy)} {F(-vbX * sx)} {F(hPt + vbY * sy)} cm\n");
        r.Children(root, Style.Default);
        return Write(r, wPt, hPt, title);
    }

    // ───────────────────────── styles ─────────────────────────

    record Style(string Fill, string Stroke, double StrokeWidth, double FillOpacity, double StrokeOpacity,
                 string? Dash, string Cap, string Join, double FontSize, string Weight, string Family, string Anchor,
                 bool StrokeUnder = false)
    {
        public static readonly Style Default = new("#000", "none", 1, 1, 1, null, "butt", "miter", 16, "normal", "sans-serif", "start");

        public Style With(XElement e)
        {
            Style s = this;
            double opacity = Num(Attr(e, "opacity"), 1);
            s = s with
            {
                Fill = Attr(e, "fill") ?? s.Fill,
                Stroke = Attr(e, "stroke") ?? s.Stroke,
                StrokeWidth = Num(Attr(e, "stroke-width"), s.StrokeWidth),
                FillOpacity = Num(Attr(e, "fill-opacity"), s.FillOpacity) * opacity,
                StrokeOpacity = Num(Attr(e, "stroke-opacity"), s.StrokeOpacity) * opacity,
                Dash = Attr(e, "stroke-dasharray") is { } d ? (d == "none" ? null : d) : s.Dash,
                Cap = Attr(e, "stroke-linecap") ?? s.Cap,
                Join = Attr(e, "stroke-linejoin") ?? s.Join,
                FontSize = Num(Attr(e, "font-size")?.Replace("px", ""), s.FontSize),
                Weight = Attr(e, "font-weight") ?? s.Weight,
                Family = Attr(e, "font-family") ?? s.Family,
                Anchor = Attr(e, "text-anchor") ?? s.Anchor,
                // paint-order="stroke": stroke drawn under the fill (used for white halos behind text).
                StrokeUnder = Attr(e, "paint-order") is { } po ? po.TrimStart().StartsWith("stroke") : s.StrokeUnder,
            };
            return s;
        }
    }

    // ───────────────────────── renderer ─────────────────────────

    sealed class Renderer
    {
        public readonly StringBuilder Out = new();
        public readonly Dictionary<string, XElement> Markers = new(), Clips = new();
        public readonly Dictionary<(double, double), string> Alpha = new();
        public readonly SortedSet<int> FontsUsed = new();

        public void Children(XElement parent, Style st)
        {
            foreach (XElement c in parent.Elements()) Element(c, st);
        }

        void Element(XElement e, Style inherited)
        {
            string name = e.Name.LocalName;
            if (name is "defs" or "marker" or "clipPath" or "title" or "desc" or "pattern" or "filter") return;
            if (Attr(e, "display") == "none" || Attr(e, "visibility") == "hidden") return;
            Style st = inherited.With(e);
            string? tf = Attr(e, "transform");
            string? clip = Attr(e, "clip-path");
            bool push = tf is not null || clip is not null;
            if (push) Out.Append("q\n");
            if (tf is not null) foreach (Matrix m in Transforms(tf)) Out.Append(Cm(m));
            if (clip is not null && ClipRef(clip) is { } cp && Clips.TryGetValue(cp, out XElement? clipEl))
            {
                foreach (XElement shape in clipEl.Elements())
                {
                    PathData? p = Geometry(shape);
                    if (p is null) continue;
                    Out.Append(p.Ops).Append("W n\n");
                }
            }
            switch (name)
            {
                case "svg":
                case "g":
                case "a":
                    Children(e, st);
                    break;
                case "text":
                    Text(e, st);
                    break;
                default:
                    PathData? path = Geometry(e);
                    if (path is not null)
                    {
                        Paint(path, st);
                        Markers2(e, path, st);
                    }
                    break;
            }
            if (push) Out.Append("Q\n");
        }

        /// <summary>Fill and/or stroke a built path with the current style.</summary>
        void Paint(PathData p, Style st)
        {
            (double R, double G, double B)? fill = Colour(st.Fill);
            (double R, double G, double B)? stroke = st.StrokeWidth > 0 ? Colour(st.Stroke) : null;
            if (p.IsOpenLine) fill = null; // lines have no fill area
            if (fill is null && stroke is null) return;
            Out.Append("q\n");
            Out.Append(Gs(fill is null ? 1 : st.FillOpacity, stroke is null ? 1 : st.StrokeOpacity));
            if (fill is { } f) Out.Append($"{F(f.R)} {F(f.G)} {F(f.B)} rg\n");
            if (stroke is { } s)
            {
                Out.Append($"{F(s.R)} {F(s.G)} {F(s.B)} RG {F(st.StrokeWidth)} w ");
                Out.Append(st.Cap switch { "round" => "1", "square" => "2", _ => "0" }).Append(" J ");
                Out.Append(st.Join switch { "round" => "1", "bevel" => "2", _ => "0" }).Append(" j\n");
                List<double> dash = st.Dash is null ? new List<double>() : Nums(st.Dash);
                if (dash.Count > 0 && dash.Any(x => x > 0))
                {
                    if (dash.Count % 2 == 1) dash.AddRange(dash);
                    Out.Append("[").Append(string.Join(" ", dash.Select(F))).Append("] 0 d\n");
                }
            }
            Out.Append(p.Ops);
            Out.Append(fill is not null && stroke is not null ? "B\n" : fill is not null ? "f\n" : "S\n");
            Out.Append("Q\n");
        }

        string Gs(double fill, double stroke)
        {
            if (fill >= 0.999 && stroke >= 0.999) return "";
            (double, double) key = (Math.Round(fill, 3), Math.Round(stroke, 3));
            if (!Alpha.TryGetValue(key, out string? n)) Alpha[key] = n = $"GS{Alpha.Count + 1}";
            return $"/{n} gs\n";
        }

        /// <summary>marker-start / marker-end, oriented along the path (auto-start-reverse).</summary>
        void Markers2(XElement e, PathData p, Style st)
        {
            void Draw(string? attr, (double X, double Y, double A)? at, bool start)
            {
                if (attr is null || at is null || ClipRef(attr) is not { } id || !Markers.TryGetValue(id, out XElement? m)) return;
                List<double> vb = Nums(Attr(m, "viewBox") ?? "0 0 3 3");
                double mw = Num(Attr(m, "markerWidth"), 3), mh = Num(Attr(m, "markerHeight"), 3);
                double refX = Num(Attr(m, "refX"), 0), refY = Num(Attr(m, "refY"), 0);
                double scaleUnits = Attr(m, "markerUnits") == "userSpaceOnUse" ? 1 : st.StrokeWidth;
                double kx = vb.Count == 4 ? mw / vb[2] : 1, ky = vb.Count == 4 ? mh / vb[3] : 1;
                double k = Math.Min(kx, ky) * scaleUnits;
                string orient = Attr(m, "orient") ?? "0";
                double angle = orient switch
                {
                    "auto" => at.Value.A,
                    "auto-start-reverse" => start ? at.Value.A + 180 : at.Value.A,
                    _ => Num(orient, 0),
                };
                Out.Append("q\n");
                Out.Append(Cm(Matrix.Translate(at.Value.X, at.Value.Y)));
                Out.Append(Cm(Matrix.Rotate(angle)));
                Out.Append(Cm(Matrix.Scale(k, k)));
                Out.Append(Cm(Matrix.Translate(-refX, -refY)));
                Children(m, Style.Default with { StrokeWidth = 1 });
                Out.Append("Q\n");
            }
            Draw(Attr(e, "marker-start"), p.Start, true);
            Draw(Attr(e, "marker-end"), p.End, false);
        }

        // ───────── text ─────────

        void Text(XElement e, Style st)
        {
            double x = Num(Attr(e, "x"), 0), y = Num(Attr(e, "y"), 0);
            bool preserve = e.Attributes().Any(a => a.Name.LocalName == "space" && a.Value == "preserve");
            foreach (XNode node in e.Nodes())
            {
                if (node is XText t)
                {
                    Run(t.Value, ref x, y, st, preserve);
                }
                else if (node is XElement span && span.Name.LocalName == "tspan")
                {
                    Style ss = st.With(span);
                    if (Attr(span, "x") is { } sx) x = Num(sx, x);
                    if (Attr(span, "y") is { } sy) y = Num(sy, y);
                    x += Num(Attr(span, "dx"), 0);
                    y += Num(Attr(span, "dy"), 0);
                    Run(span.Value, ref x, y, ss, preserve);
                }
            }
        }

        void Run(string text, ref double x, double y, Style st, bool preserve)
        {
            text = text.Replace("\r", "").Replace("\n", preserve ? " " : " ").Replace("\t", " ");
            if (!preserve) text = System.Text.RegularExpressions.Regex.Replace(text, " {2,}", " ").Trim();
            if (text.Length == 0) return;
            (double R, double G, double B)? fill = Colour(st.Fill);
            int font = Fonts.Pick(st.Family, st.Weight);
            FontsUsed.Add(font);
            byte[] bytes = Fonts.Encode(text);
            double width = Fonts.Width(font, bytes) * st.FontSize / 1000;
            double startX = st.Anchor switch { "middle" => x - width / 2, "end" => x - width, _ => x };
            // Halo first: text outline stroked underneath (only for paint-order="stroke", so inherited strokes don't outline other text).
            if (st.StrokeUnder && st.StrokeWidth > 0 && Colour(st.Stroke) is { } sc)
            {
                Out.Append("q\n").Append(Gs(1, st.StrokeOpacity));
                Out.Append($"{F(sc.R)} {F(sc.G)} {F(sc.B)} RG {F(st.StrokeWidth)} w ");
                Out.Append(st.Join switch { "round" => "1", "bevel" => "2", _ => "0" }).Append(" j\n");
                Out.Append($"BT 1 Tr /F{font} {F(st.FontSize)} Tf 1 0 0 -1 {F(startX)} {F(y)} Tm ");
                TextString(bytes);
                Out.Append(" Tj ET\nQ\n");
            }
            if (fill is { } f)
            {
                Out.Append("q\n").Append(Gs(st.FillOpacity, 1));
                Out.Append($"{F(f.R)} {F(f.G)} {F(f.B)} rg\n");
                // Undo the page's y flip so glyphs stand upright.
                Out.Append($"BT /F{font} {F(st.FontSize)} Tf 1 0 0 -1 {F(startX)} {F(y)} Tm ");
                TextString(bytes);
                Out.Append(" Tj ET\nQ\n");
            }
            x = startX + width;
        }

        /// <summary>A PDF string literal for encoded text.</summary>
        void TextString(byte[] bytes)
        {
            Out.Append('(');
            foreach (byte b in bytes)
            {
                if (b is (byte)'(' or (byte)')' or (byte)'\\') Out.Append('\\').Append((char)b);
                else if (b < 32 || b > 126) Out.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
                else Out.Append((char)b);
            }
            Out.Append(')');
        }

        // ───────── shapes → path operators ─────────

        PathData? Geometry(XElement e)
        {
            switch (e.Name.LocalName)
            {
                case "path":
                    return PathData.Parse(Attr(e, "d") ?? "");
                case "rect":
                {
                    double x = Num(Attr(e, "x"), 0), y = Num(Attr(e, "y"), 0);
                    double w = Num(Attr(e, "width"), 0), h = Num(Attr(e, "height"), 0);
                    if (w <= 0 || h <= 0) return null;
                    double rx = Num(Attr(e, "rx"), -1), ry = Num(Attr(e, "ry"), -1);
                    if (rx < 0) rx = ry; if (ry < 0) ry = rx;
                    rx = Math.Clamp(rx, 0, w / 2); ry = Math.Clamp(ry, 0, h / 2);
                    if (rx <= 0 || ry <= 0)
                        return PathData.Parse($"M{F(x)},{F(y)} H{F(x + w)} V{F(y + h)} H{F(x)} Z");
                    return PathData.Parse(
                        $"M{F(x + rx)},{F(y)} H{F(x + w - rx)} A{F(rx)},{F(ry)} 0 0 1 {F(x + w)},{F(y + ry)} " +
                        $"V{F(y + h - ry)} A{F(rx)},{F(ry)} 0 0 1 {F(x + w - rx)},{F(y + h)} H{F(x + rx)} " +
                        $"A{F(rx)},{F(ry)} 0 0 1 {F(x)},{F(y + h - ry)} V{F(y + ry)} A{F(rx)},{F(ry)} 0 0 1 {F(x + rx)},{F(y)} Z");
                }
                case "circle":
                case "ellipse":
                {
                    double cx = Num(Attr(e, "cx"), 0), cy = Num(Attr(e, "cy"), 0);
                    double rx = e.Name.LocalName == "circle" ? Num(Attr(e, "r"), 0) : Num(Attr(e, "rx"), 0);
                    double ry = e.Name.LocalName == "circle" ? rx : Num(Attr(e, "ry"), 0);
                    if (rx <= 0 || ry <= 0) return null;
                    return PathData.Parse($"M{F(cx + rx)},{F(cy)} A{F(rx)},{F(ry)} 0 0 1 {F(cx - rx)},{F(cy)} A{F(rx)},{F(ry)} 0 0 1 {F(cx + rx)},{F(cy)} Z");
                }
                case "line":
                {
                    PathData p = PathData.Parse($"M{F(Num(Attr(e, "x1"), 0))},{F(Num(Attr(e, "y1"), 0))} L{F(Num(Attr(e, "x2"), 0))},{F(Num(Attr(e, "y2"), 0))}");
                    p.IsOpenLine = true;
                    return p;
                }
                case "polyline":
                case "polygon":
                {
                    List<double> n = Nums(Attr(e, "points") ?? "");
                    if (n.Count < 4) return null;
                    StringBuilder sb = new StringBuilder($"M{F(n[0])},{F(n[1])}");
                    for (int i = 2; i + 1 < n.Count; i += 2) sb.Append($" L{F(n[i])},{F(n[i + 1])}");
                    if (e.Name.LocalName == "polygon") sb.Append(" Z");
                    return PathData.Parse(sb.ToString());
                }
                default:
                    return null;
            }
        }
    }

    // ───────────────────────── paths ─────────────────────────

    sealed class PathData
    {
        public string Ops = "";
        public bool IsOpenLine;
        public (double X, double Y, double A)? Start, End;

        public static PathData Parse(string d)
        {
            PathData p = new PathData();
            StringBuilder sb = new StringBuilder();
            PathTokens tok = new PathTokens(d);
            double cx = 0, cy = 0, sx = 0, sy = 0;     // current point, subpath start
            double lcx = 0, lcy = 0;                   // last control point (for S/T)
            char prev = ' ';
            char cmd = ' ';
            double lastDirX = 0, lastDirY = 0;
            bool firstSeg = true;

            void Seg(double fromX, double fromY, double c1x, double c1y, double c2x, double c2y, double toX, double toY, bool curve)
            {
                // Start tangent of the very first segment and end tangent of the latest one (for markers).
                if (firstSeg)
                {
                    (double dx, double dy) = curve ? Dir(fromX, fromY, c1x, c1y, c2x, c2y, toX, toY) : (toX - fromX, toY - fromY);
                    if (dx != 0 || dy != 0) { p.Start = (fromX, fromY, Math.Atan2(dy, dx) * 180 / Math.PI); firstSeg = false; }
                }
                (double ex, double ey) = curve ? Dir(toX, toY, c2x, c2y, c1x, c1y, fromX, fromY) : (fromX - toX, fromY - toY);
                if (ex != 0 || ey != 0) { lastDirX = -ex; lastDirY = -ey; }
                p.End = (toX, toY, Math.Atan2(lastDirY, lastDirX) * 180 / Math.PI);
                if (curve) sb.Append($"{F(c1x)} {F(c1y)} {F(c2x)} {F(c2y)} {F(toX)} {F(toY)} c\n");
                else sb.Append($"{F(toX)} {F(toY)} l\n");
            }

            while (tok.More())
            {
                if (tok.PeekCommand() is { } c) { cmd = c; tok.Skip(); }
                else if (cmd == ' ') break;
                bool rel = char.IsLower(cmd);
                double ox = rel ? cx : 0, oy = rel ? cy : 0;
                switch (char.ToUpper(cmd))
                {
                    case 'M':
                        cx = ox + tok.Num(); cy = oy + tok.Num(); sx = cx; sy = cy;
                        sb.Append($"{F(cx)} {F(cy)} m\n");
                        cmd = rel ? 'l' : 'L'; // following pairs are line-tos
                        prev = 'M';
                        lcx = cx; lcy = cy;
                        continue;
                    case 'L':
                    {
                        double x = ox + tok.Num(), y = oy + tok.Num();
                        Seg(cx, cy, 0, 0, 0, 0, x, y, false); cx = x; cy = y;
                        break;
                    }
                    case 'H':
                    {
                        double x = ox + tok.Num();
                        Seg(cx, cy, 0, 0, 0, 0, x, cy, false); cx = x;
                        break;
                    }
                    case 'V':
                    {
                        double y = oy + tok.Num();
                        Seg(cx, cy, 0, 0, 0, 0, cx, y, false); cy = y;
                        break;
                    }
                    case 'C':
                    {
                        double x1 = ox + tok.Num(), y1 = oy + tok.Num(), x2 = ox + tok.Num(), y2 = oy + tok.Num(), x = ox + tok.Num(), y = oy + tok.Num();
                        Seg(cx, cy, x1, y1, x2, y2, x, y, true); lcx = x2; lcy = y2; cx = x; cy = y;
                        prev = 'C';
                        continue;
                    }
                    case 'S':
                    {
                        double x1 = prev == 'C' ? 2 * cx - lcx : cx, y1 = prev == 'C' ? 2 * cy - lcy : cy;
                        double x2 = ox + tok.Num(), y2 = oy + tok.Num(), x = ox + tok.Num(), y = oy + tok.Num();
                        Seg(cx, cy, x1, y1, x2, y2, x, y, true); lcx = x2; lcy = y2; cx = x; cy = y;
                        prev = 'C';
                        continue;
                    }
                    case 'Q':
                    {
                        double qx = ox + tok.Num(), qy = oy + tok.Num(), x = ox + tok.Num(), y = oy + tok.Num();
                        Quad(qx, qy, x, y);
                        continue;
                    }
                    case 'T':
                    {
                        double qx = prev == 'Q' ? 2 * cx - lcx : cx, qy = prev == 'Q' ? 2 * cy - lcy : cy;
                        double x = ox + tok.Num(), y = oy + tok.Num();
                        Quad(qx, qy, x, y);
                        continue;
                    }
                    case 'A':
                    {
                        double rx = tok.Num(), ry = tok.Num(), rot = tok.Num();
                        bool large = tok.Flag(), sweep = tok.Flag();
                        double x = ox + tok.Num(), y = oy + tok.Num();
                        foreach (double[] b in Arc(cx, cy, rx, ry, rot, large, sweep, x, y))
                            Seg(b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7], true);
                        if (rx == 0 || ry == 0 || (cx == x && cy == y)) { if (cx != x || cy != y) Seg(cx, cy, 0, 0, 0, 0, x, y, false); }
                        cx = x; cy = y;
                        break;
                    }
                    case 'Z':
                        if (cx != sx || cy != sy) Seg(cx, cy, 0, 0, 0, 0, sx, sy, false);
                        sb.Append("h\n");
                        cx = sx; cy = sy;
                        prev = 'Z';
                        cmd = ' ';
                        continue;
                    default:
                        tok.Skip();
                        continue;
                }
                prev = char.ToUpper(cmd);
                lcx = cx; lcy = cy;

                void Quad(double qx, double qy, double x, double y)
                {
                    Seg(cx, cy, cx + 2.0 / 3 * (qx - cx), cy + 2.0 / 3 * (qy - cy), x + 2.0 / 3 * (qx - x), y + 2.0 / 3 * (qy - y), x, y, true);
                    lcx = qx; lcy = qy; cx = x; cy = y; prev = 'Q';
                }
            }
            p.Ops = sb.ToString();
            return p;
        }

        /// <summary>Direction leaving (x0,y0) along a cubic, skipping coincident control points.</summary>
        static (double, double) Dir(double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3)
        {
            const double eps = 1e-9;
            if (Math.Abs(x1 - x0) > eps || Math.Abs(y1 - y0) > eps) return (x1 - x0, y1 - y0);
            if (Math.Abs(x2 - x0) > eps || Math.Abs(y2 - y0) > eps) return (x2 - x0, y2 - y0);
            return (x3 - x0, y3 - y0);
        }

        /// <summary>SVG elliptical arc → cubic Béziers (endpoint to centre parameterisation, ≤ 90° pieces).</summary>
        static IEnumerable<double[]> Arc(double x1, double y1, double rx, double ry, double phiDeg, bool large, bool sweep, double x2, double y2)
        {
            if (rx == 0 || ry == 0 || (x1 == x2 && y1 == y2)) yield break;
            rx = Math.Abs(rx); ry = Math.Abs(ry);
            double phi = phiDeg * Math.PI / 180, cos = Math.Cos(phi), sin = Math.Sin(phi);
            double dx = (x1 - x2) / 2, dy = (y1 - y2) / 2;
            double x1p = cos * dx + sin * dy, y1p = -sin * dx + cos * dy;
            double lambda = x1p * x1p / (rx * rx) + y1p * y1p / (ry * ry);
            if (lambda > 1) { double s = Math.Sqrt(lambda); rx *= s; ry *= s; }
            double num = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p;
            double den = rx * rx * y1p * y1p + ry * ry * x1p * x1p;
            double coef = Math.Sqrt(Math.Max(0, num / den)) * (large == sweep ? -1 : 1);
            double cxp = coef * rx * y1p / ry, cyp = -coef * ry * x1p / rx;
            double cx = cos * cxp - sin * cyp + (x1 + x2) / 2, cy = sin * cxp + cos * cyp + (y1 + y2) / 2;
            static double Ang(double ux, double uy, double vx, double vy)
            {
                double a = Math.Atan2(ux * vy - uy * vx, ux * vx + uy * vy);
                return a;
            }
            double t1 = Ang(1, 0, (x1p - cxp) / rx, (y1p - cyp) / ry);
            double dt = Ang((x1p - cxp) / rx, (y1p - cyp) / ry, (-x1p - cxp) / rx, (-y1p - cyp) / ry);
            if (!sweep && dt > 0) dt -= 2 * Math.PI;
            if (sweep && dt < 0) dt += 2 * Math.PI;
            int n = Math.Max(1, (int)Math.Ceiling(Math.Abs(dt) / (Math.PI / 2) - 1e-9));
            double step = dt / n, k = 4.0 / 3 * Math.Tan(step / 4);
            (double, double) Pt(double t) { double ex = rx * Math.Cos(t), ey = ry * Math.Sin(t); return (cos * ex - sin * ey + cx, sin * ex + cos * ey + cy); }
            (double, double) Dv(double t) { double ex = -rx * Math.Sin(t), ey = ry * Math.Cos(t); return (cos * ex - sin * ey, sin * ex + cos * ey); }
            double t0 = t1;
            (double px, double py) = (x1, y1);
            for (int i = 0; i < n; i++)
            {
                double ta = t0 + i * step, tb = ta + step;
                (double ax, double ay) = i == 0 ? (x1, y1) : Pt(ta);
                (double bx, double by) = i == n - 1 ? (x2, y2) : Pt(tb);
                (double dax, double day) = Dv(ta);
                (double dbx, double dby) = Dv(tb);
                yield return new[] { ax, ay, ax + k * dax, ay + k * day, bx - k * dbx, by - k * dby, bx, by };
            }
        }
    }

    sealed class PathTokens
    {
        readonly string s;
        int i;
        public PathTokens(string s) { this.s = s; }
        void Ws() { while (i < s.Length && (char.IsWhiteSpace(s[i]) || s[i] == ',')) i++; }
        public bool More() { Ws(); return i < s.Length; }
        public char? PeekCommand() { Ws(); return i < s.Length && char.IsLetter(s[i]) && s[i] != 'e' && s[i] != 'E' ? s[i] : null; }
        public void Skip() => i++;
        public bool Flag() { Ws(); if (i < s.Length && (s[i] == '0' || s[i] == '1')) return s[i++] == '1'; return Num() != 0; }
        public double Num()
        {
            Ws();
            int start = i;
            if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
            bool dot = false;
            while (i < s.Length && (char.IsDigit(s[i]) || (s[i] == '.' && !dot)))
            {
                if (s[i] == '.') dot = true;
                i++;
            }
            if (i < s.Length && (s[i] == 'e' || s[i] == 'E'))
            {
                int save = i++;
                if (i < s.Length && (s[i] == '+' || s[i] == '-')) i++;
                if (i < s.Length && char.IsDigit(s[i])) while (i < s.Length && char.IsDigit(s[i])) i++;
                else i = save;
            }
            if (i == start) { i++; return 0; } // junk: skip a character rather than loop forever
            return double.TryParse(s.AsSpan(start, i - start), NumberStyles.Float, Inv, out double v) ? v : 0;
        }
    }

    // ───────────────────────── transforms ─────────────────────────

    readonly record struct Matrix(double A, double B, double C, double D, double E, double F)
    {
        public static Matrix Translate(double x, double y) => new(1, 0, 0, 1, x, y);
        public static Matrix Scale(double x, double y) => new(x, 0, 0, y, 0, 0);
        public static Matrix Rotate(double deg)
        {
            double r = deg * Math.PI / 180;
            return new(Math.Cos(r), Math.Sin(r), -Math.Sin(r), Math.Cos(r), 0, 0);
        }
    }

    static string Cm(Matrix m) => $"{F(m.A)} {F(m.B)} {F(m.C)} {F(m.D)} {F(m.E)} {F(m.F)} cm\n";

    /// <summary>SVG transform list in the order written (each applied inside the previous).</summary>
    static IEnumerable<Matrix> Transforms(string t)
    {
        foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(t, @"(\w+)\s*\(([^)]*)\)"))
        {
            List<double> a = Nums(m.Groups[2].Value);
            switch (m.Groups[1].Value)
            {
                case "translate": yield return Matrix.Translate(a.ElementAtOrDefault(0), a.ElementAtOrDefault(1)); break;
                case "scale": yield return Matrix.Scale(a.ElementAtOrDefault(0), a.Count > 1 ? a[1] : a.ElementAtOrDefault(0)); break;
                case "rotate":
                    if (a.Count >= 3)
                    {
                        yield return Matrix.Translate(a[1], a[2]);
                        yield return Matrix.Rotate(a[0]);
                        yield return Matrix.Translate(-a[1], -a[2]);
                    }
                    else yield return Matrix.Rotate(a.ElementAtOrDefault(0));
                    break;
                case "matrix" when a.Count == 6: yield return new Matrix(a[0], a[1], a[2], a[3], a[4], a[5]); break;
                case "skewX": yield return new Matrix(1, 0, Math.Tan(a.ElementAtOrDefault(0) * Math.PI / 180), 1, 0, 0); break;
                case "skewY": yield return new Matrix(1, Math.Tan(a.ElementAtOrDefault(0) * Math.PI / 180), 0, 1, 0, 0); break;
            }
        }
    }

    // ───────────────────────── small parsers ─────────────────────────

    static string? Attr(XElement e, string name)
    {
        string? v = e.Attribute(name)?.Value;
        if (v is null && e.Attribute("style")?.Value is { } style)
        {
            foreach (string part in style.Split(';'))
            {
                string[] kv = part.Split(':', 2);
                if (kv.Length == 2 && kv[0].Trim() == name) return kv[1].Trim();
            }
        }
        return v;
    }

    static double Num(string? s, double fallback) =>
        s is not null && double.TryParse(s.Trim().TrimEnd('%'), NumberStyles.Float, Inv, out double v) ? v : fallback;

    static List<double> Nums(string s)
    {
        PathTokens t = new PathTokens(s);
        List<double> list = new List<double>();
        while (t.More()) list.Add(t.Num());
        return list;
    }

    static string? ClipRef(string v)
    {
        Match m = System.Text.RegularExpressions.Regex.Match(v, @"url\(\s*#([^)\s]+)\s*\)");
        return m.Success ? m.Groups[1].Value : null;
    }

    /// <summary>Width/height attribute in points (mm or px); px value returned too.</summary>
    static double Length(string? s, out double px)
    {
        px = 0;
        if (string.IsNullOrWhiteSpace(s)) return 0;
        s = s.Trim();
        if (s.EndsWith("mm")) { double mm = Num(s[..^2], 0); px = mm * 96 / 25.4; return mm * PtPerMm; }
        if (s.EndsWith("cm")) { double cm = Num(s[..^2], 0); px = cm * 960 / 25.4; return cm * 10 * PtPerMm; }
        if (s.EndsWith("in")) { double inch = Num(s[..^2], 0); px = inch * 96; return inch * 72; }
        if (s.EndsWith("pt")) { double pt = Num(s[..^2], 0); px = pt / PtPerPx; return pt; }
        if (s.EndsWith("%")) return 0;
        px = Num(s.Replace("px", ""), 0);
        return px * PtPerPx;
    }

    static readonly Dictionary<string, string> Named = new(StringComparer.OrdinalIgnoreCase)
    {
        ["black"] = "#000000", ["white"] = "#ffffff", ["red"] = "#ff0000", ["green"] = "#008000", ["blue"] = "#0000ff",
        ["gray"] = "#808080", ["grey"] = "#808080", ["yellow"] = "#ffff00", ["orange"] = "#ffa500",
    };

    static (double R, double G, double B)? Colour(string? c)
    {
        if (string.IsNullOrWhiteSpace(c)) return null;
        c = c.Trim();
        if (c is "none" or "transparent" || c.StartsWith("url(")) return null;
        if (Named.TryGetValue(c, out string? hex)) c = hex;
        if (c.StartsWith('#'))
        {
            string h = c[1..];
            if (h.Length is 3 or 4) h = string.Concat(h.Take(3).Select(ch => $"{ch}{ch}"));
            if (h.Length >= 6 && int.TryParse(h[..6], NumberStyles.HexNumber, Inv, out int v))
                return (((v >> 16) & 255) / 255.0, ((v >> 8) & 255) / 255.0, (v & 255) / 255.0);
            return null;
        }
        Match m = System.Text.RegularExpressions.Regex.Match(c, @"rgba?\(([^)]*)\)");
        if (m.Success)
        {
            double[] n = m.Groups[1].Value.Split(',').Select(x => Num(x, 0)).ToArray();
            if (n.Length >= 3) return (n[0] / 255, n[1] / 255, n[2] / 255);
        }
        return (0, 0, 0);
    }

    // ───────────────────────── fonts ─────────────────────────

    static class Fonts
    {
        // F1 Helvetica, F2 Helvetica-Bold, F3 Times-Roman, F4 Times-Bold, F5 Courier, F6 Courier-Bold.
        public static readonly string[] BaseNames = { "", "Helvetica", "Helvetica-Bold", "Times-Roman", "Times-Bold", "Courier", "Courier-Bold" };

        public static int Pick(string family, string weight)
        {
            string f = family.ToLowerInvariant();
            bool bold = weight is "bold" or "bolder" || (int.TryParse(weight, out int w) && w >= 600);
            int baseFont = f.Contains("mono") || f.Contains("consolas") || f.Contains("courier") ? 5
                : (f.Contains("serif") && !f.Contains("sans-serif")) || f.Contains("georgia") || f.Contains("times") ? 3
                : 1;
            return baseFont + (bold ? 1 : 0);
        }

        static readonly Encoding Cp1252 = Win1252.Instance;

        public static byte[] Encode(string s) => Cp1252.GetBytes(s);

        public static double Width(int font, byte[] bytes)
        {
            if (font >= 5) return bytes.Length * 600.0;
            short[] table = font switch { 1 => Helvetica, 2 => HelveticaBold, 3 => Times, _ => TimesBold };
            double w = 0;
            foreach (byte b in bytes)
            {
                short v = b >= 32 ? table[b - 32] : (short)0;
                w += v > 0 ? v : 556;
            }
            return w;
        }

        // Advance widths (1/1000 em) for WinAnsi codes 32–255, from the Adobe core font metrics (AFM).
        static readonly short[] Helvetica = { 278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556, 1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556, 333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556, 556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584, 0, 0, 0, 222, 556, 333, 1000, 556, 556, 333, 1000, 667, 333, 1000, 0, 611, 0, 0, 222, 222, 333, 333, 350, 556, 1000, 333, 1000, 500, 333, 944, 0, 500, 667, 278, 333, 556, 556, 556, 556, 260, 556, 333, 737, 370, 556, 584, 333, 737, 333, 400, 584, 0, 0, 333, 556, 537, 278, 333, 0, 365, 556, 834, 834, 834, 611, 667, 667, 667, 667, 667, 667, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278, 722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611, 556, 556, 556, 556, 556, 556, 889, 500, 556, 556, 556, 556, 278, 278, 278, 278, 556, 556, 556, 556, 556, 556, 556, 584, 611, 556, 556, 556, 556, 500, 556, 500 };
        static readonly short[] HelveticaBold = { 278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278, 556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611, 975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778, 667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556, 333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611, 611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584, 0, 0, 0, 278, 556, 500, 1000, 556, 556, 333, 1000, 667, 333, 1000, 0, 611, 0, 0, 278, 278, 500, 500, 350, 556, 1000, 333, 1000, 556, 333, 944, 0, 500, 667, 278, 333, 556, 556, 556, 556, 280, 556, 333, 737, 370, 556, 584, 333, 737, 333, 400, 584, 0, 0, 333, 611, 556, 278, 333, 0, 365, 556, 834, 834, 834, 611, 722, 722, 722, 722, 722, 722, 1000, 722, 667, 667, 667, 667, 278, 278, 278, 278, 722, 722, 778, 778, 778, 778, 778, 584, 778, 722, 722, 722, 722, 667, 667, 611, 556, 556, 556, 556, 556, 556, 889, 556, 556, 556, 556, 556, 278, 278, 278, 278, 611, 611, 611, 611, 611, 611, 611, 584, 611, 611, 611, 611, 611, 556, 611, 556 };
        static readonly short[] Times = { 250, 333, 408, 500, 500, 833, 778, 180, 333, 333, 500, 564, 250, 333, 250, 278, 500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 278, 278, 564, 564, 564, 444, 921, 722, 667, 667, 722, 611, 556, 722, 722, 333, 389, 722, 611, 889, 722, 722, 556, 722, 667, 556, 611, 722, 722, 944, 722, 722, 611, 333, 278, 333, 469, 500, 333, 444, 500, 444, 500, 444, 333, 500, 500, 278, 278, 500, 278, 778, 500, 500, 500, 500, 333, 389, 278, 500, 500, 722, 500, 500, 444, 480, 200, 480, 541, 0, 0, 0, 333, 500, 444, 1000, 500, 500, 333, 1000, 556, 333, 889, 0, 611, 0, 0, 333, 333, 444, 444, 350, 500, 1000, 333, 980, 389, 333, 722, 0, 444, 722, 250, 333, 500, 500, 500, 500, 200, 500, 333, 760, 276, 500, 564, 333, 760, 333, 400, 564, 0, 0, 333, 500, 453, 250, 333, 0, 310, 500, 750, 750, 750, 444, 722, 722, 722, 722, 722, 722, 889, 667, 611, 611, 611, 611, 333, 333, 333, 333, 722, 722, 722, 722, 722, 722, 722, 564, 722, 722, 722, 722, 722, 722, 556, 500, 444, 444, 444, 444, 444, 444, 667, 444, 444, 444, 444, 444, 278, 278, 278, 278, 500, 500, 500, 500, 500, 500, 500, 564, 500, 500, 500, 500, 500, 500, 500, 500 };
        static readonly short[] TimesBold = { 250, 333, 555, 500, 500, 1000, 833, 278, 333, 333, 500, 570, 250, 333, 250, 278, 500, 500, 500, 500, 500, 500, 500, 500, 500, 500, 333, 333, 570, 570, 570, 500, 930, 722, 667, 722, 722, 667, 611, 778, 778, 389, 500, 778, 667, 944, 722, 778, 611, 778, 722, 556, 667, 722, 722, 1000, 722, 722, 667, 333, 278, 333, 581, 500, 333, 500, 556, 444, 556, 444, 333, 500, 556, 278, 333, 556, 278, 833, 556, 500, 556, 556, 444, 389, 333, 556, 500, 722, 500, 500, 444, 394, 220, 394, 520, 0, 0, 0, 333, 500, 500, 1000, 500, 500, 333, 1000, 556, 333, 1000, 0, 667, 0, 0, 333, 333, 500, 500, 350, 500, 1000, 333, 1000, 389, 333, 722, 0, 444, 722, 250, 333, 500, 500, 500, 500, 220, 500, 333, 747, 300, 500, 570, 333, 747, 333, 400, 570, 0, 0, 333, 556, 540, 250, 333, 0, 330, 500, 750, 750, 750, 500, 722, 722, 722, 722, 722, 722, 1000, 722, 667, 667, 667, 667, 389, 389, 389, 389, 722, 722, 778, 778, 778, 778, 778, 570, 778, 722, 722, 722, 722, 722, 611, 556, 500, 500, 500, 500, 500, 500, 722, 444, 444, 444, 444, 444, 278, 278, 278, 278, 500, 556, 500, 500, 500, 500, 500, 570, 500, 556, 556, 556, 556, 500, 556, 500 };
    }

    /// <summary>Windows-1252 encoder that doesn't need the CodePages provider; unknown characters become '?'.</summary>
    sealed class Win1252 : Encoding
    {
        public static readonly Win1252 Instance = new();
        static readonly Dictionary<char, byte> High = new()
        {
            ['€'] = 0x80, ['‚'] = 0x82, ['ƒ'] = 0x83, ['„'] = 0x84, ['…'] = 0x85, ['†'] = 0x86, ['‡'] = 0x87, ['ˆ'] = 0x88,
            ['‰'] = 0x89, ['Š'] = 0x8A, ['‹'] = 0x8B, ['Œ'] = 0x8C, ['Ž'] = 0x8E, ['‘'] = 0x91, ['’'] = 0x92, ['“'] = 0x93,
            ['”'] = 0x94, ['•'] = 0x95, ['–'] = 0x96, ['—'] = 0x97, ['˜'] = 0x98, ['™'] = 0x99, ['š'] = 0x9A, ['›'] = 0x9B,
            ['œ'] = 0x9C, ['ž'] = 0x9E, ['Ÿ'] = 0x9F,
            ['→'] = (byte)'>', ['←'] = (byte)'<', ['✓'] = (byte)'v', ['✕'] = (byte)'x', ['×'] = 0xD7,
        };
        public override int GetByteCount(char[] chars, int index, int count) => count;
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            for (int k = 0; k < charCount; k++)
            {
                char ch = chars[charIndex + k];
                bytes[byteIndex + k] = ch < 0x80 || (ch >= 0xA0 && ch <= 0xFF) ? (byte)ch
                    : High.TryGetValue(ch, out byte b) ? b : (byte)'?';
            }
            return charCount;
        }
        public override int GetCharCount(byte[] bytes, int index, int count) => count;
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            for (int k = 0; k < byteCount; k++) chars[charIndex + k] = (char)bytes[byteIndex + k];
            return byteCount;
        }
        public override int GetMaxByteCount(int charCount) => charCount;
        public override int GetMaxCharCount(int byteCount) => byteCount;
    }

    // ───────────────────────── file writer ─────────────────────────

    static byte[] Write(Renderer r, double wPt, double hPt, string title)
    {
        byte[] content = Encoding.ASCII.GetBytes(r.Out.ToString());
        byte[] packed;
        using (MemoryStream ms = new MemoryStream())
        {
            using (ZLibStream z = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true)) z.Write(content);
            packed = ms.ToArray();
        }

        List<byte[]> objects = new List<byte[]>();
        int Add(string s) { objects.Add(Encoding.ASCII.GetBytes(s)); return objects.Count; }
        int AddRaw(byte[] b) { objects.Add(b); return objects.Count; }

        // 1 catalog, 2 pages, 3 page, 4 content, then fonts, graphics states and info.
        Add("<< /Type /Catalog /Pages 2 0 R >>");
        Add("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        Add(""); // page — filled in once resource object numbers are known
        byte[] head = Encoding.ASCII.GetBytes($"<< /Length {packed.Length} /Filter /FlateDecode >>\nstream\n");
        byte[] tail = Encoding.ASCII.GetBytes("\nendstream");
        AddRaw(head.Concat(packed).Concat(tail).ToArray());

        StringBuilder fontRefs = new StringBuilder();
        foreach (int f in r.FontsUsed)
        {
            int n = Add($"<< /Type /Font /Subtype /Type1 /BaseFont /{Fonts.BaseNames[f]} /Encoding /WinAnsiEncoding >>");
            fontRefs.Append($"/F{f} {n} 0 R ");
        }
        StringBuilder gsRefs = new StringBuilder();
        foreach (((double, double) key, string? name) in r.Alpha)
        {
            int n = Add($"<< /Type /ExtGState /ca {F(key.Item1)} /CA {F(key.Item2)} >>");
            gsRefs.Append($"/{name} {n} 0 R ");
        }
        int info = Add($"<< /Title {PdfString(title)} /Producer (BeDiagram) /CreationDate (D:{DateTime.UtcNow:yyyyMMddHHmmss}Z) >>");

        string resources = "<< /ProcSet [/PDF /Text]" +
            (fontRefs.Length > 0 ? $" /Font << {fontRefs}>>" : "") +
            (gsRefs.Length > 0 ? $" /ExtGState << {gsRefs}>>" : "") + " >>";
        objects[2] = Encoding.ASCII.GetBytes($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {F(wPt)} {F(hPt)}] /Resources {resources} /Contents 4 0 R >>");

        using MemoryStream outStream = new MemoryStream();
        void W(string s) { byte[] b = Encoding.ASCII.GetBytes(s); outStream.Write(b); }
        W("%PDF-1.4\n");
        outStream.Write(new byte[] { (byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n' });
        List<long> offsets = new List<long>();
        for (int k = 0; k < objects.Count; k++)
        {
            offsets.Add(outStream.Position);
            W($"{k + 1} 0 obj\n");
            outStream.Write(objects[k]);
            W("\nendobj\n");
        }
        long xref = outStream.Position;
        W($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (long o in offsets) W($"{o:D10} 00000 n \n");
        W($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R /Info {info} 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return outStream.ToArray();
    }

    static string PdfString(string s)
    {
        StringBuilder sb = new StringBuilder("(");
        foreach (byte b in Win1252.Instance.GetBytes(s ?? ""))
        {
            if (b is (byte)'(' or (byte)')' or (byte)'\\') sb.Append('\\').Append((char)b);
            else if (b < 32 || b > 126) sb.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
            else sb.Append((char)b);
        }
        return sb.Append(')').ToString();
    }
}
