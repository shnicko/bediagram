using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static BeDiagram.Services.Export.Pdf.PdfSyntax;
using static BeDiagram.Services.Export.Pdf.SvgValues;

namespace BeDiagram.Services.Export.Pdf;

/// <summary>
/// Walks the SVG elements and writes the PDF content stream: groups, transforms, clip paths, shapes (painted with
/// fill / stroke / dash / opacity), markers and text. Records the fonts and opacity states it used, for the file.
/// </summary>
internal sealed class PdfRenderer
{
    public readonly StringBuilder Out = new();
    public readonly Dictionary<string, XElement> Markers = new(), Clips = new();
    public readonly Dictionary<(double, double), string> Alpha = new();
    public readonly SortedSet<int> FontsUsed = new();
    public readonly PdfFonts Fonts = new();

    public void Children(XElement parent, PdfStyle st)
    {
        foreach (XElement c in parent.Elements()) Element(c, st);
    }

    void Element(XElement e, PdfStyle inherited)
    {
        string name = e.Name.LocalName;
        if (name is "defs" or "marker" or "clipPath" or "title" or "desc" or "pattern" or "filter") return;
        if (Attr(e, "display") == "none" || Attr(e, "visibility") == "hidden") return;
        PdfStyle st = inherited.With(e);
        string? tf = Attr(e, "transform");
        string? clip = Attr(e, "clip-path");
        bool push = tf is not null || clip is not null;
        if (push) Out.Append("q\n");
        if (tf is not null) foreach (PdfMatrix m in PdfMatrix.Parse(tf)) Out.Append(m.Cm);
        if (clip is not null && ClipRef(clip) is { } cp && Clips.TryGetValue(cp, out XElement? clipEl))
        {
            foreach (XElement shape in clipEl.Elements())
            {
                SvgPath? p = ShapePath(shape);
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
                SvgPath? path = ShapePath(e);
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
    void Paint(SvgPath p, PdfStyle st)
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
    void Markers2(XElement e, SvgPath p, PdfStyle st)
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
            Out.Append(PdfMatrix.Translate(at.Value.X, at.Value.Y).Cm);
            Out.Append(PdfMatrix.Rotate(angle).Cm);
            Out.Append(PdfMatrix.Scale(k, k).Cm);
            Out.Append(PdfMatrix.Translate(-refX, -refY).Cm);
            Children(m, PdfStyle.Default with { StrokeWidth = 1 });
            Out.Append("Q\n");
        }
        Draw(Attr(e, "marker-start"), p.Start, true);
        Draw(Attr(e, "marker-end"), p.End, false);
    }

    // ───────── text ─────────

    void Text(XElement e, PdfStyle st)
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
                PdfStyle ss = st.With(span);
                if (Attr(span, "x") is { } sx) x = Num(sx, x);
                if (Attr(span, "y") is { } sy) y = Num(sy, y);
                x += Num(Attr(span, "dx"), 0);
                y += Num(Attr(span, "dy"), 0);
                Run(span.Value, ref x, y, ss, preserve);
            }
        }
    }

    void Run(string text, ref double x, double y, PdfStyle st, bool preserve)
    {
        text = text.Replace("\r", "").Replace("\n", preserve ? " " : " ").Replace("\t", " ");
        if (!preserve) text = Regex.Replace(text, " {2,}", " ").Trim();
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

    /// <summary>A shape element (path, rect, circle, ellipse, line, polyline, polygon) as path operators, or null.</summary>
    SvgPath? ShapePath(XElement e)
    {
        switch (e.Name.LocalName)
        {
            case "path":
                return SvgPath.Parse(Attr(e, "d") ?? "");
            case "rect":
            {
                double x = Num(Attr(e, "x"), 0), y = Num(Attr(e, "y"), 0);
                double w = Num(Attr(e, "width"), 0), h = Num(Attr(e, "height"), 0);
                if (w <= 0 || h <= 0) return null;
                double rx = Num(Attr(e, "rx"), -1), ry = Num(Attr(e, "ry"), -1);
                if (rx < 0) rx = ry; if (ry < 0) ry = rx;
                rx = Math.Clamp(rx, 0, w / 2); ry = Math.Clamp(ry, 0, h / 2);
                if (rx <= 0 || ry <= 0)
                    return SvgPath.Parse($"M{F(x)},{F(y)} H{F(x + w)} V{F(y + h)} H{F(x)} Z");
                return SvgPath.Parse(
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
                return SvgPath.Parse($"M{F(cx + rx)},{F(cy)} A{F(rx)},{F(ry)} 0 0 1 {F(cx - rx)},{F(cy)} A{F(rx)},{F(ry)} 0 0 1 {F(cx + rx)},{F(cy)} Z");
            }
            case "line":
            {
                SvgPath p = SvgPath.Parse($"M{F(Num(Attr(e, "x1"), 0))},{F(Num(Attr(e, "y1"), 0))} L{F(Num(Attr(e, "x2"), 0))},{F(Num(Attr(e, "y2"), 0))}");
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
                return SvgPath.Parse(sb.ToString());
            }
            default:
                return null;
        }
    }
}
