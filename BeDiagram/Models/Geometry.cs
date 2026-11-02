using System.Globalization;
using System.Text;

namespace BeDiagram.Models;

/// <summary>Shape and connector geometry shared by the canvas and the SVG exporter.</summary>
public static class Geometry
{
    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>The outline actually drawn: the type's shape, or for outcome nodes the outcome's own shape.</summary>
    public static string ShapeOf(Node n) =>
        n.Type == NodeTypes.Outcome ? Outcomes.Get(n.Outcome).Shape : NodeTypes.Get(n.Type).Shape;

    /// <summary>The icon drawn in a node: the outcome glyph for outcome nodes, otherwise the chosen icon.</summary>
    public static string? IconOf(Node n) => n.Type == NodeTypes.Outcome ? Outcomes.Get(n.Outcome).Glyph : n.Icon;

    /// <summary>Icon stroke width in 24-unit icon space (outcome glyphs are bolder).</summary>
    public static double IconStroke(Node n) => n.Type == NodeTypes.Outcome ? 2.75 : 2;

    /// <summary>Shapes whose label sits underneath (icon in the middle): circles with an icon, and every outcome.</summary>
    public static bool LabelBelow(Node n) =>
        n.Type == NodeTypes.Outcome || (ShapeOf(n) == "circle" && Icons.Exists(n.Icon));

    public static string ShapePath(Node n)
    {
        double x = n.X, y = n.Y, w = n.W, h = n.H;
        switch (ShapeOf(n))
        {
            case "pill":
            {
                var r = h / 2;
                return $"M{F(x + r)},{F(y)} H{F(x + w - r)} A{F(r)},{F(r)} 0 0 1 {F(x + w - r)},{F(y + h)} H{F(x + r)} A{F(r)},{F(r)} 0 0 1 {F(x + r)},{F(y)} Z";
            }
            case "diamond":
                return $"M{F(x + w / 2)},{F(y)} L{F(x + w)},{F(y + h / 2)} L{F(x + w / 2)},{F(y + h)} L{F(x)},{F(y + h / 2)} Z";
            case "parallelogram":
            {
                var k = Math.Min(18, w / 6);
                return $"M{F(x + k)},{F(y)} H{F(x + w)} L{F(x + w - k)},{F(y + h)} H{F(x)} Z";
            }
            case "hexagon":
            {
                var k = Math.Min(20, w / 5);
                return $"M{F(x + k)},{F(y)} H{F(x + w - k)} L{F(x + w)},{F(y + h / 2)} L{F(x + w - k)},{F(y + h)} H{F(x + k)} L{F(x)},{F(y + h / 2)} Z";
            }
            case "circle":
            {
                var r = Math.Min(w, h) / 2; var cx = x + w / 2; var cy = y + h / 2;
                return $"M{F(cx - r)},{F(cy)} A{F(r)},{F(r)} 0 1 1 {F(cx + r)},{F(cy)} A{F(r)},{F(r)} 0 1 1 {F(cx - r)},{F(cy)} Z";
            }
            case "seal":
                return SealPath(x + w / 2, y + h / 2, Math.Min(w, h) / 2);
            case "octagon":
            case "triangle":
                return PolyPath(Outline(n, ShapeOf(n)));
            case "square-rounded":
            {
                var s = Math.Min(w, h); var sx = x + (w - s) / 2; var sy = y + (h - s) / 2;
                return RoundRect(sx, sy, s, s, s * 0.22);
            }
            case "rect-rounded":
                return RoundRect(x, y, w, h, 14);
            default: // rect, check
                return RoundRect(x, y, w, h, 5);
        }
    }

    /// <summary>Scalloped seal: a ring of semicircular bumps whose tips touch radius <paramref name="r"/>.</summary>
    public static string SealPath(double cx, double cy, double r)
    {
        const int bumps = 16;
        var step = 2 * Math.PI / bumps;
        // Chord points sit inside so that each bump's tip reaches r.
        var r0 = r / (Math.Cos(step / 2) + Math.Sin(step / 2));
        var bump = r0 * Math.Sin(step / 2);
        var sb = new StringBuilder();
        for (int i = 0; i <= bumps; i++)
        {
            var a = -Math.PI / 2 + i * step;
            var (px, py) = (cx + r0 * Math.Cos(a), cy + r0 * Math.Sin(a));
            sb.Append(i == 0 ? $"M{F(px)},{F(py)}" : $" A{F(bump)},{F(bump)} 0 0 1 {F(px)},{F(py)}");
        }
        return sb.Append(" Z").ToString();
    }

    static string PolyPath(List<(double X, double Y)> pts) => PathD(pts) + " Z";

    public static string RoundRect(double x, double y, double w, double h, double r)
    {
        r = Math.Min(r, Math.Min(w, h) / 2);
        return $"M{F(x + r)},{F(y)} H{F(x + w - r)} Q{F(x + w)},{F(y)} {F(x + w)},{F(y + r)} V{F(y + h - r)} Q{F(x + w)},{F(y + h)} {F(x + w - r)},{F(y + h)} H{F(x + r)} Q{F(x)},{F(y + h)} {F(x)},{F(y + h - r)} V{F(y + r)} Q{F(x)},{F(y)} {F(x + r)},{F(y)} Z";
    }

    /// <summary>Where a node's label and icon go. Shared by the canvas and the SVG export.</summary>
    public record LabelLayout(List<string> Lines, double TextX, double TextY, bool Below,
                              bool HasIcon, double IconX, double IconY, double IconSize);

    public const double LineHeight = 16;

    public static LabelLayout Layout(Node n, string label)
    {
        var t = NodeTypes.Get(n.Type);
        double cx = n.X + n.W / 2, cy = n.Y + n.H / 2;
        bool icon = Icons.Exists(n.Icon);

        if (n.Type == NodeTypes.Outcome)
        {
            // Glyph centred in the outline, label underneath. A triangle's visual centre is lower down.
            var shape = ShapeOf(n);
            var m = Math.Min(n.W, n.H);
            var size = m * (shape == "triangle" ? 0.38 : shape == "seal" ? 0.44 : 0.48);
            var gy = shape == "triangle" ? n.Y + n.H * 0.62 : cy;
            var lines = Wrap(label, Math.Max(n.W * 1.7, 130));
            return new(lines, cx, n.Y + n.H + 18, true, true, cx - size / 2, gy - size / 2, size);
        }

        if (t.Shape == "circle")
        {
            if (icon)
            {
                // Presentation style: icon centred in the circle, label underneath.
                var size = Math.Min(n.W, n.H) * 0.46;
                var lines = Wrap(label, Math.Max(n.W * 1.7, 130));
                return new(lines, cx, n.Y + n.H + 18, true, true, cx - size / 2, cy - size / 2, size);
            }
            var inner = Wrap(label, n.W * 0.78);
            return new(inner, cx, cy - (inner.Count - 1) * LineHeight / 2 + 4, false, false, 0, 0, 0);
        }

        if (icon && (t.Shape == "diamond" || t.Shape == "hexagon"))
        {
            // Narrow-sided shapes: icon above the text.
            var lines = Wrap(label, n.W * (t.Shape == "diamond" ? 0.55 : 0.7));
            var block = 22 + lines.Count * LineHeight;
            var top = cy - block / 2;
            return new(lines, cx, top + 22 + 12, false, true, cx - 10, top, 20);
        }

        if (icon)
        {
            // Icon on the left, text centred in the remaining space.
            var left = n.X + (t.Shape == "check" ? 18 : t.Shape == "parallelogram" ? 22 : 14);
            var lines = Wrap(label, n.X + n.W - (left + 26) - 10);
            var textX = (left + 26 + n.X + n.W - 8) / 2;
            return new(lines, textX, cy - (lines.Count - 1) * LineHeight / 2 + 4, false, true, left, cy - 10, 20);
        }

        var wrapped = Wrap(label, n.W * (t.Shape == "diamond" ? 0.62 : 0.86));
        return new(wrapped, cx, cy - (wrapped.Count - 1) * LineHeight / 2 + 4, false, false, 0, 0, 0);
    }

    /// <summary>One positioned line of text-box text (baseline coordinates).</summary>
    public record TextLine(string Text, double X, double Y);

    /// <summary>Where a text box's lines go, plus the SVG text-anchor. Shared by the canvas and the SVG export.</summary>
    public record TextBoxLayout(List<TextLine> Lines, string Anchor, double LineHeight, double ContentHeight);

    public static TextBoxLayout Layout(TextBox t)
    {
        var size = Math.Max(4, t.FontSize);
        var lh = size * TextLook.LineSpacing;
        var charW = size * TextLook.CharWidth(t.Font) * (t.Bold ? 1.06 : 1);
        var lines = Wrap(t.Text, Math.Max(10, t.W - 2 * TextLook.PadX), charW);
        var block = lines.Count * lh;
        var top = t.VAlign switch
        {
            TextLook.Middle => t.Y + (t.H - block) / 2,
            TextLook.Bottom => t.Y + t.H - TextLook.PadY - block,
            _ => t.Y + TextLook.PadY,
        };
        var (x, anchor) = t.Align switch
        {
            TextLook.Center => (t.X + t.W / 2, "middle"),
            TextLook.Right => (t.X + t.W - TextLook.PadX, "end"),
            _ => (t.X + TextLook.PadX, "start"),
        };
        // Baseline sits roughly 0.8 em below the top of the line box.
        var first = top + (lh - size) / 2 + size * 0.8;
        var placed = lines.Select((l, i) => new TextLine(l, x, first + i * lh)).ToList();
        return new TextBoxLayout(placed, anchor, lh, block + 2 * TextLook.PadY);
    }

    /// <summary>Outline of a text box (square or slightly rounded rectangle).</summary>
    public static string TextBoxPath(TextBox t, double radius) =>
        radius > 0 ? RoundRect(t.X, t.Y, t.W, t.H, radius) : $"M{F(t.X)},{F(t.Y)} H{F(t.X + t.W)} V{F(t.Y + t.H)} H{F(t.X)} Z";

    /// <summary>The small diagonal "folded corner" drawn on comments.</summary>
    public static string FoldPath(TextBox t) => $"M{F(t.X + t.W - 14)},{F(t.Y + t.H)} L{F(t.X + t.W)},{F(t.Y + t.H - 14)}";

    /// <summary>Bounding box of everything on the canvas (null when empty).</summary>
    public static (double X, double Y, double W, double H)? ContentBounds(Diagram d)
    {
        var boxes = d.Nodes.Cast<IBox>().Concat(d.TextBoxes).Concat(d.Groups).ToList();
        if (boxes.Count == 0) return null;
        double minX = boxes.Min(b => b.X), minY = boxes.Min(b => b.Y);
        double maxX = boxes.Max(b => b.X + b.W), maxY = boxes.Max(b => b.Y + b.H + ExtraBelow(b));
        // Labels drawn under circles and outcomes can be wider than the shape.
        foreach (var n in d.Nodes.Where(n => ExtraBelow(n) > 0))
        {
            var half = Math.Max(n.W * 1.7, 130) / 2;
            minX = Math.Min(minX, n.X + n.W / 2 - half);
            maxX = Math.Max(maxX, n.X + n.W / 2 + half);
        }
        return (minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>Bounds that wrap a set of boxes plus padding (with room for a group's title bar).</summary>
    public static (double X, double Y, double W, double H) GroupBounds(IEnumerable<IBox> boxes)
    {
        var list = boxes.ToList();
        double minX = list.Min(b => b.X), minY = list.Min(b => b.Y);
        double maxX = list.Max(b => b.X + b.W), maxY = list.Max(b => b.Y + b.H + ExtraBelow(b));
        return (minX - Group.Pad, minY - Group.Pad - Group.Header, maxX - minX + Group.Pad * 2, maxY - minY + Group.Pad * 2 + Group.Header);
    }

    /// <summary>Space taken under a box by an external label (circle with icon).</summary>
    public static double ExtraBelow(IBox b) =>
        b is Node n && LabelBelow(n) ? 34 : 0;

    public record EdgeGeom(string D, double LabelX, double LabelY, double LabelAngle);

    /// <summary>Full connector geometry: path plus where (and at what angle) its label sits.</summary>
    public static EdgeGeom EdgeGeometry(IBox a, IBox b, string style, string labelOrient = LabelOrients.Horizontal, double labelPos = 0.5)
    {
        var pts = Route(a, b, style);
        var (x, y, seg) = PointAt(pts, labelPos);
        return new EdgeGeom(PathD(pts), x, y, LabelAngle(labelOrient, seg));
    }

    /// <summary>Connector geometry for a stored edge, honouring pinned sides and a dragged bend.</summary>
    public static EdgeGeom EdgeGeometry(IBox a, IBox b, Edge e)
    {
        var r = Route(a, b, e);
        var (x, y, seg) = PointAt(r.Points, e.LabelPos);
        return new EdgeGeom(PathD(r.Points), x, y, LabelAngle(e.LabelOrient, seg));
    }

    /// <summary>
    /// A routed connector. <see cref="BendIndex"/> is the segment (Points[i] → Points[i+1]) the user can drag
    /// sideways, or -1 when there isn't one. <see cref="FromSide"/>/<see cref="ToSide"/> are the sides actually
    /// used (null for an automatic straight end, which aims at the other shape rather than a side).
    /// </summary>
    public record EdgeRoute(List<(double X, double Y)> Points, int BendIndex, string? FromSide, string? ToSide)
    {
        public bool BendIsVertical => BendIndex >= 0 && Math.Abs(Points[BendIndex].X - Points[BendIndex + 1].X) < 0.5;
        public (double X, double Y) BendHandle => BendIndex < 0 ? (0, 0)
            : ((Points[BendIndex].X + Points[BendIndex + 1].X) / 2, (Points[BendIndex].Y + Points[BendIndex + 1].Y) / 2);
    }

    public static EdgeRoute Route(IBox a, IBox b, Edge e) =>
        Route(a, b, e.Style, Sides.IsValid(e.FromSide) ? e.FromSide : null, Sides.IsValid(e.ToSide) ? e.ToSide : null, e.Bend);

    public static List<(double X, double Y)> Route(IBox a, IBox b, string style) => Route(a, b, style, null, null, 0).Points;

    public static EdgeRoute Route(IBox a, IBox b, string style, string? fromSide, string? toSide, double bend)
    {
        if (style == EdgeStyles.Straight) return StraightRoute(a, b, fromSide, toSide);
        if (fromSide is null && toSide is null)
        {
            var (s1, s2) = AutoSides(a, b);
            return ElbowRoute(a, b, s1, s2, bend, legacy: true);
        }
        if (fromSide is not null && toSide is not null) return ElbowRoute(a, b, fromSide, toSide, bend, legacy: false);

        // One end pinned: pick the other end's side that gives the shortest, simplest route.
        EdgeRoute? best = null;
        double bestScore = double.MaxValue;
        foreach (var s in Sides.All)
        {
            var r = ElbowRoute(a, b, fromSide ?? s, toSide ?? s, bend, legacy: false);
            var score = Length(r.Points) + 25 * r.Points.Count;
            if (score < bestScore) { bestScore = score; best = r; }
        }
        return best!;
    }

    /// <summary>Middle of a side of the shape's box — the same spots as the ● ports.</summary>
    public static (double X, double Y) SidePoint(IBox b, string side) => side switch
    {
        Sides.Top => (b.X + b.W / 2, b.Y),
        Sides.Right => (b.X + b.W, b.Y + b.H / 2),
        Sides.Bottom => (b.X + b.W / 2, b.Y + b.H),
        _ => (b.X, b.Y + b.H / 2),
    };

    /// <summary>Straight line between the shapes' outlines, aimed centre to centre; pinned ends start at a side middle.</summary>
    static EdgeRoute StraightRoute(IBox a, IBox b, string? fromSide, string? toSide)
    {
        (double X, double Y) ca = (a.X + a.W / 2, a.Y + a.H / 2), cb = (b.X + b.W / 2, b.Y + b.H / 2);
        (double X, double Y)? p1 = fromSide is null ? null : SidePoint(a, fromSide);
        (double X, double Y)? p2 = toSide is null ? null : SidePoint(b, toSide);
        var aimA = p2 ?? cb;
        var aimB = p1 ?? ca;
        var start = p1 ?? Boundary(a, aimA.X, aimA.Y);
        var end = p2 ?? Boundary(b, aimB.X, aimB.Y);
        return new EdgeRoute(new() { start, end }, -1, fromSide, toSide);
    }

    /// <summary>The sides the automatic elbow uses: vertical flow if the target is clearly above/below, otherwise horizontal.</summary>
    public static (string From, string To) AutoSides(IBox a, IBox b)
    {
        double dx = (b.X + b.W / 2) - (a.X + a.W / 2), dy = (b.Y + b.H / 2) - (a.Y + a.H / 2);
        bool vertical = Math.Abs(dy) - (a.H + b.H) / 2 > Math.Abs(dx) - (a.W + b.W) / 2 - 20
                        && Math.Abs(dy) > (a.H + b.H) / 2;
        return vertical
            ? (dy > 0 ? (Sides.Bottom, Sides.Top) : (Sides.Top, Sides.Bottom))
            : (dx > 0 ? (Sides.Right, Sides.Left) : (Sides.Left, Sides.Right));
    }

    const double Stub = 20;

    /// <summary>
    /// Right-angled connector from a side of <paramref name="a"/> to a side of <paramref name="b"/>.
    /// Shapes: Z (facing sides), U (same side), L (perpendicular, corner ahead of both), or a 4–5 segment
    /// detour when the target is behind a side. <paramref name="legacy"/> keeps the original automatic
    /// behaviour (always a Z between facing sides, even when the shapes overlap).
    /// </summary>
    static EdgeRoute ElbowRoute(IBox a, IBox b, string s1, string s2, double bend, bool legacy)
    {
        var p1 = SidePoint(a, s1);
        var p2 = SidePoint(b, s2);
        var d1 = Sides.Dir(s1);
        var d2 = Sides.Dir(s2);
        List<(double X, double Y)> pts;
        int idx;

        double Dot((double X, double Y) u, (double X, double Y) v) => u.X * v.X + u.Y * v.Y;
        (double X, double Y) Add((double X, double Y) p, (double X, double Y) d, double k) => (p.X + d.X * k, p.Y + d.Y * k);
        (double X, double Y) Sub((double X, double Y) p, (double X, double Y) q) => (p.X - q.X, p.Y - q.Y);
        // Extent of a box along a unit axis.
        (double Min, double Max) Span(IBox box, (double X, double Y) ax)
        {
            double v1 = Dot((box.X, box.Y), ax), v2 = Dot((box.X + box.W, box.Y + box.H), ax);
            return (Math.Min(v1, v2), Math.Max(v1, v2));
        }

        bool opposite = d1.X == -d2.X && d1.Y == -d2.Y;
        bool same = d1 == d2;

        if (opposite)
        {
            double gap = Dot(Sub(p2, p1), d1);
            if (gap > 0 || legacy)
            {
                // Z: out, across at half way, in.
                var m1 = Add(p1, d1, gap / 2);
                var m2 = Add(p2, d2, gap / 2);
                pts = new() { p1, m1, m2, p2 };
                idx = 1;
            }
            else
            {
                // Target is behind the start side: step out, cross over between (or around) the shapes, step back in.
                (double X, double Y) n = (Math.Abs(d1.Y), Math.Abs(d1.X)); // perpendicular axis
                var (aMin, aMax) = Span(a, n);
                var (bMin, bMax) = Span(b, n);
                double lane = aMax < bMin ? (aMax + bMin) / 2
                            : bMax < aMin ? (bMax + aMin) / 2
                            : Math.Max(aMax, bMax) + Stub;
                var q1 = Add(p1, d1, Stub);
                var q2 = Add(p2, d2, Stub);
                double o1 = lane - Dot(q1, n), o2 = lane - Dot(q2, n);
                pts = new() { p1, q1, Add(q1, n, o1), Add(q2, n, o2), q2, p2 };
                idx = 2;
            }
        }
        else if (same)
        {
            // U: out past whichever end sticks out further, across, back in.
            double level = Math.Max(Dot(p1, d1), Dot(p2, d1)) + Stub;
            var q1 = Add(p1, d1, level - Dot(p1, d1));
            var q2 = Add(p2, d2, level - Dot(p2, d2));
            pts = new() { p1, q1, q2, p2 };
            idx = 1;
        }
        else
        {
            double t1 = Dot(Sub(p2, p1), d1); // how far ahead of the start side the end point is
            double t2 = Dot(Sub(p1, p2), d2); // how far ahead of the end side the start point is
            if (t1 > Stub / 2 && t2 > Stub / 2)
            {
                // L: a single corner.
                pts = new() { p1, Add(p1, d1, t1), p2 };
                idx = -1;
            }
            else
            {
                // Step out of the start side (half way to the target's box when there's room), run across, step into the end side.
                double ahead = Span(b, d1).Min - Dot(p1, d1); // gap to the target box's near face
                double out1 = ahead > 2 * Stub ? ahead / 2 : Stub;
                var q1 = Add(p1, d1, out1);
                var q2 = Add(p2, d2, Stub);
                var x = Add(q1, d2, Dot(Sub(q2, q1), d2));
                pts = new() { p1, q1, x, q2, p2 };
                idx = 1;
            }
        }

        if (idx >= 0 && bend != 0)
        {
            bool vertical = Math.Abs(pts[idx].X - pts[idx + 1].X) < 0.5;
            for (int k = idx; k <= idx + 1; k++)
                pts[k] = vertical ? (pts[k].X + bend, pts[k].Y) : (pts[k].X, pts[k].Y + bend);
        }
        else if (idx == 1 && pts.Count == 4 && bend == 0
                 && Math.Abs(pts[1].X - pts[2].X) < 1 && Math.Abs(pts[1].Y - pts[2].Y) < 1)
        {
            // Ends line up: one straight run, as before.
            pts = new() { p1, p2 };
            idx = -1;
        }
        return new EdgeRoute(pts, idx, s1, s2);
    }

    static double Length(List<(double X, double Y)> pts)
    {
        double len = 0;
        for (int i = 1; i < pts.Count; i++) len += Math.Abs(pts[i].X - pts[i - 1].X) + Math.Abs(pts[i].Y - pts[i - 1].Y);
        return len;
    }

    /// <summary>Where a ray from the box centre towards (tx, ty) leaves the shape's outline.</summary>
    public static (double X, double Y) Boundary(IBox box, double tx, double ty)
    {
        double cx = box.X + box.W / 2, cy = box.Y + box.H / 2;
        double dx = tx - cx, dy = ty - cy;
        if (Math.Abs(dx) < 1e-6 && Math.Abs(dy) < 1e-6) return (cx, cy);
        var shape = box is Node n ? ShapeOf(n) : "rect";

        if (shape is "circle" or "seal")
        {
            var r = Math.Min(box.W, box.H) / 2;
            var len = Math.Sqrt(dx * dx + dy * dy);
            return (cx + dx / len * r, cy + dy / len * r);
        }

        var poly = Outline(box, shape);
        double best = double.MaxValue;
        for (int i = 0; i < poly.Count; i++)
        {
            var (p1x, p1y) = poly[i];
            var (p2x, p2y) = poly[(i + 1) % poly.Count];
            // Solve centre + t*(dx,dy) = p1 + u*(p2-p1)
            double ex = p2x - p1x, ey = p2y - p1y;
            double den = dx * ey - dy * ex;
            if (Math.Abs(den) < 1e-9) continue;
            double t = ((p1x - cx) * ey - (p1y - cy) * ex) / den;
            double u = ((p1x - cx) * dy - (p1y - cy) * dx) / den;
            if (t > 0 && u >= -1e-9 && u <= 1 + 1e-9 && t < best) best = t;
        }
        return best == double.MaxValue ? (cx, cy) : (cx + dx * best, cy + dy * best);
    }

    static List<(double X, double Y)> Outline(IBox b, string shape)
    {
        double x = b.X, y = b.Y, w = b.W, h = b.H;
        switch (shape)
        {
            case "octagon":
            {
                // Regular octagon (stop sign) centred in the box, flat sides touching it.
                var r = Math.Min(w, h) / 2; var k = r * Math.Tan(Math.PI / 8);
                double cx = x + w / 2, cy = y + h / 2;
                return new() { (cx - k, cy - r), (cx + k, cy - r), (cx + r, cy - k), (cx + r, cy + k),
                               (cx + k, cy + r), (cx - k, cy + r), (cx - r, cy + k), (cx - r, cy - k) };
            }
            case "triangle":
                return new() { (x + w / 2, y), (x + w, y + h), (x, y + h) };
            case "square-rounded":
            {
                var s = Math.Min(w, h);
                return new() { (x + (w - s) / 2, y + (h - s) / 2), (x + (w + s) / 2, y + (h - s) / 2), (x + (w + s) / 2, y + (h + s) / 2), (x + (w - s) / 2, y + (h + s) / 2) };
            }
            case "diamond":
                return new() { (x + w / 2, y), (x + w, y + h / 2), (x + w / 2, y + h), (x, y + h / 2) };
            case "parallelogram":
            {
                var k = Math.Min(18, w / 6);
                return new() { (x + k, y), (x + w, y), (x + w - k, y + h), (x, y + h) };
            }
            case "hexagon":
            {
                var k = Math.Min(20, w / 5);
                return new() { (x + k, y), (x + w - k, y), (x + w, y + h / 2), (x + w - k, y + h), (x + k, y + h), (x, y + h / 2) };
            }
            default:
                return new() { (x, y), (x + w, y), (x + w, y + h), (x, y + h) };
        }
    }

    public static string PathD(List<(double X, double Y)> pts) =>
        "M" + string.Join(" L", pts.Select(p => $"{F(p.X)},{F(p.Y)}"));

    /// <summary>Point at fraction t of the polyline's length, plus that segment's angle in degrees.</summary>
    public static (double X, double Y, double Angle) PointAt(List<(double X, double Y)> pts, double t)
    {
        t = Math.Clamp(t, 0, 1);
        var lengths = new List<double>();
        for (int i = 0; i < pts.Count - 1; i++)
            lengths.Add(Math.Sqrt(Math.Pow(pts[i + 1].X - pts[i].X, 2) + Math.Pow(pts[i + 1].Y - pts[i].Y, 2)));
        var total = lengths.Sum();
        if (total < 1e-6) return (pts[0].X, pts[0].Y, 0);
        var target = total * t;
        for (int i = 0; i < lengths.Count; i++)
        {
            if (target <= lengths[i] || i == lengths.Count - 1)
            {
                var f = lengths[i] < 1e-6 ? 0 : Math.Min(1, target / lengths[i]);
                var (ax, ay) = pts[i]; var (bx, by) = pts[i + 1];
                return (ax + (bx - ax) * f, ay + (by - ay) * f, Math.Atan2(by - ay, bx - ax) * 180 / Math.PI);
            }
            target -= lengths[i];
        }
        return (pts[^1].X, pts[^1].Y, 0);
    }

    /// <summary>Label rotation for an orientation, kept readable (never upside down).</summary>
    public static double LabelAngle(string orient, double segmentAngle)
    {
        double a = orient switch
        {
            LabelOrients.Along => segmentAngle,
            LabelOrients.Perpendicular => segmentAngle + 90,
            _ => 0,
        };
        while (a > 90) a -= 180;
        while (a <= -90) a += 180;
        return Math.Round(a, 2);
    }

    /// <summary>Approximate label box width for a single-line label.</summary>
    public static double LabelWidth(string text) => (text ?? "").Length * 7 + 12;

    /// <summary>Very simple word wrap for SVG text, based on an average character width.</summary>
    public static List<string> Wrap(string text, double width, double charWidth = 7.2)
    {
        var maxChars = Math.Max(4, (int)(width / charWidth));
        var lines = new List<string>();
        foreach (var para in (text ?? "").Replace("\r", "").Split('\n'))
        {
            var sb = new StringBuilder();
            foreach (var word in para.Split(' '))
            {
                if (sb.Length > 0 && sb.Length + 1 + word.Length > maxChars)
                {
                    lines.Add(sb.ToString());
                    sb.Clear();
                }
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(word);
            }
            lines.Add(sb.ToString());
        }
        return lines;
    }

    public static bool Contains(IBox b, double x, double y) =>
        x >= b.X && x <= b.X + b.W && y >= b.Y && y <= b.Y + b.H;

    public static bool Intersects(IBox b, double x1, double y1, double x2, double y2) =>
        b.X < Math.Max(x1, x2) && b.X + b.W > Math.Min(x1, x2) &&
        b.Y < Math.Max(y1, y2) && b.Y + b.H > Math.Min(y1, y2);
}
