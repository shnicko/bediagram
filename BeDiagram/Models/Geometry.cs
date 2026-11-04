using System.Globalization;
using System.Text;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Shapes;
using BeDiagram.Models.Styles;

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

    /// <summary>The outline strategy a node is drawn with (see <see cref="IShapeOutline"/>).</summary>
    public static IShapeOutline OutlineOf(Node n) => ShapeOutlines.Get(ShapeOf(n));

    /// <summary>The outline of any box: a node's own outline, a plain rectangle for text boxes and groups.</summary>
    public static IShapeOutline OutlineOf(IBox b) => b is Node n ? OutlineOf(n) : ShapeOutlines.Rect;

    /// <summary>Shapes whose label sits underneath (icon in the middle): circles with an icon, and every outcome.</summary>
    public static bool LabelBelow(Node n) =>
        n.Type == NodeTypes.Outcome || (OutlineOf(n).IconMovesLabelBelow && Icons.Exists(n.Icon));

    public static string ShapePath(Node n) => OutlineOf(n).Path(n);

    /// <summary>Scalloped seal: a ring of semicircular bumps whose tips touch radius <paramref name="r"/>.</summary>
    public static string SealPath(double cx, double cy, double r)
    {
        const int bumps = 16;
        double step = 2 * Math.PI / bumps;
        // Chord points sit inside so that each bump's tip reaches r.
        double r0 = r / (Math.Cos(step / 2) + Math.Sin(step / 2));
        double bump = r0 * Math.Sin(step / 2);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i <= bumps; i++)
        {
            double a = -Math.PI / 2 + i * step;
            (double px, double py) = (cx + r0 * Math.Cos(a), cy + r0 * Math.Sin(a));
            sb.Append(i == 0 ? $"M{F(px)},{F(py)}" : $" A{F(bump)},{F(bump)} 0 0 1 {F(px)},{F(py)}");
        }
        return sb.Append(" Z").ToString();
    }

    public static string RoundRect(double x, double y, double w, double h, double r)
    {
        r = Math.Min(r, Math.Min(w, h) / 2);
        return $"M{F(x + r)},{F(y)} H{F(x + w - r)} Q{F(x + w)},{F(y)} {F(x + w)},{F(y + r)} V{F(y + h - r)} Q{F(x + w)},{F(y + h)} {F(x + w - r)},{F(y + h)} H{F(x + r)} Q{F(x)},{F(y + h)} {F(x)},{F(y + h - r)} V{F(y + r)} Q{F(x)},{F(y)} {F(x + r)},{F(y)} Z";
    }

    // ───────────── Link badge ─────────────

    /// <summary>Radius of the round "has a more detailed diagram" badge.</summary>
    public const double BadgeR = 9;

    /// <summary>True when the item links to another diagram.</summary>
    public static bool HasLink(IBox b) => !string.IsNullOrWhiteSpace(b switch { Node n => n.Link, Group g => g.Link, _ => null });

    /// <summary>
    /// Centre of the link badge: the bottom-right of the drawn outline (on the curve for circles, seals and
    /// octagons, on the lower-right edge for diamonds), or tucked inside the bottom-right corner of a group.
    /// </summary>
    public static (double X, double Y) LinkBadgeCentre(IBox b) =>
        b is Node n ? OutlineOf(n).LinkBadgeCentre(n) : (b.X + b.W - 18, b.Y + b.H - 18);

    /// <summary>SVG markup for the link badge (white disc, coloured ring, Lucide "link" glyph). Used by canvas and export.</summary>
    public static string LinkBadgeSvg(IBox b, string colour)
    {
        (double cx, double cy) = LinkBadgeCentre(b);
        const double glyph = 12;
        return $"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(BadgeR)}\" fill=\"#ffffff\" stroke=\"{colour}\" stroke-width=\"1.4\"/>"
             + $"<g transform=\"translate({F(cx - glyph / 2)},{F(cy - glyph / 2)}) scale({F(glyph / 24)})\" fill=\"none\" stroke=\"{colour}\" stroke-width=\"2.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Icons.Get("link")}</g>";
    }

    public const double LineHeight = 16;
    public const double DescFontSize = 11;
    public const double DescLineHeight = 14;
    /// <summary>Average character width of the 11px description text (for wrapping).</summary>
    const double DescCharWidth = 6.1;

    /// <summary>The label shown for a node: its own label, or the type name when it has none.</summary>
    public static string LabelText(Node n) => string.IsNullOrWhiteSpace(n.Label) ? NodeTypes.Get(n.Type).Display : n.Label;

    /// <summary>
    /// Wrap width for text drawn outside the shape (a label under a circle/outcome, and the description).
    /// The node's own <see cref="Node.TextWidth"/> when set, otherwise a little wider than the shape.
    /// </summary>
    public static double TextWidth(Node n) =>
        n.TextWidth is > 0 ? n.TextWidth.Value : DefaultTextWidth(n);

    public static double DefaultTextWidth(Node n) => LabelBelow(n) ? Math.Max(n.W * 1.7, 130) : Math.Max(n.W, 160);

    public static LabelLayout Layout(Node n) => Layout(n, LabelText(n));

    public static LabelLayout Layout(Node n, string label)
    {
        LabelLayout lay = LayoutLabel(n, label);
        if (n.Description is not { } desc) return lay;
        // Under the label when the label is below the shape; otherwise just under the shape.
        List<string> lines = Wrap(desc, TextWidth(n), DescCharWidth);
        double firstBaseline = lay.Below
            ? lay.TextY + (lay.Lines.Count - 1) * LineHeight + 4 + DescLineHeight
            : n.Y + n.H + 6 + DescLineHeight;
        return lay with { Desc = lines, DescX = n.X + n.W / 2, DescY = firstBaseline };
    }

    /// <summary>
    /// The white backing behind text drawn outside the shape (label below and/or description), or null when nothing
    /// is drawn outside. One rectangle covers both so they read as one block.
    /// </summary>
    public static (double X, double Y, double W, double H)? OutsideTextBox(Node n, LabelLayout lay)
    {
        if (!lay.Below && lay.Desc.Count == 0) return null;
        double cx = n.X + n.W / 2;
        double w = 0, top, bottom;
        if (lay.Below)
        {
            w = lay.Lines.Max(l => l.Length) * 7.2 + 14;
            top = lay.TextY - 14;
            bottom = top + lay.Lines.Count * LineHeight + 6;
        }
        else
        {
            top = n.Y + n.H + 4;
            bottom = top;
        }
        if (lay.Desc.Count > 0)
        {
            w = Math.Max(w, lay.Desc.Max(l => l.Length) * DescCharWidth + 14);
            bottom = lay.DescY + (lay.Desc.Count - 1) * DescLineHeight + 5;
        }
        return (cx - w / 2, top, w, bottom - top);
    }

    static LabelLayout LayoutLabel(Node n, string label)
    {
        IShapeOutline outline = OutlineOf(n);
        double cx = n.X + n.W / 2, cy = n.Y + n.H / 2;
        bool icon = Icons.Exists(n.Icon);

        if (n.Type == NodeTypes.Outcome)
        {
            // Glyph centred in the outline, label underneath. A triangle's visual centre is lower down.
            double size = Math.Min(n.W, n.H) * outline.GlyphScale;
            double gy = n.Y + n.H * outline.GlyphCentreY;
            List<string> lines = Wrap(label, TextWidth(n));
            return new(lines, cx, n.Y + n.H + 18, true, true, cx - size / 2, gy - size / 2, size);
        }

        if (icon && outline.IconMovesLabelBelow)
        {
            // Presentation style: icon centred in the circle, label underneath.
            double size = Math.Min(n.W, n.H) * 0.46;
            List<string> lines = Wrap(label, TextWidth(n));
            return new(lines, cx, n.Y + n.H + 18, true, true, cx - size / 2, cy - size / 2, size);
        }

        if (icon && outline.IconAboveTextWrap is double aboveWrap)
        {
            // Narrow-sided shapes: icon above the text.
            List<string> lines = Wrap(label, n.W * aboveWrap);
            double block = 22 + lines.Count * LineHeight;
            double top = cy - block / 2;
            return new(lines, cx, top + 22 + 12, false, true, cx - 10, top, 20);
        }

        if (icon)
        {
            // Icon on the left, text centred in the remaining space.
            double left = n.X + outline.IconInset;
            List<string> lines = Wrap(label, n.X + n.W - (left + 26) - 10);
            double textX = (left + 26 + n.X + n.W - 8) / 2;
            return new(lines, textX, cy - (lines.Count - 1) * LineHeight / 2 + 4, false, true, left, cy - 10, 20);
        }

        List<string> wrapped = Wrap(label, n.W * outline.TextWrap);
        return new(wrapped, cx, cy - (wrapped.Count - 1) * LineHeight / 2 + 4, false, false, 0, 0, 0);
    }

    public static TextBoxLayout Layout(TextBox t)
    {
        double size = Math.Max(4, t.FontSize);
        double lh = size * TextLook.LineSpacing;
        double charW = size * TextLook.CharWidth(t.Font) * (t.Bold ? 1.06 : 1);
        List<string> lines = Wrap(t.Text, Math.Max(10, t.W - 2 * TextLook.PadX), charW);
        double block = lines.Count * lh;
        double top = t.VAlign switch
        {
            TextLook.Middle => t.Y + (t.H - block) / 2,
            TextLook.Bottom => t.Y + t.H - TextLook.PadY - block,
            _ => t.Y + TextLook.PadY,
        };
        (double x, string? anchor) = t.Align switch
        {
            TextLook.Center => (t.X + t.W / 2, "middle"),
            TextLook.Right => (t.X + t.W - TextLook.PadX, "end"),
            _ => (t.X + TextLook.PadX, "start"),
        };
        // Baseline sits roughly 0.8 em below the top of the line box.
        double first = top + (lh - size) / 2 + size * 0.8;
        List<TextLine> placed = lines.Select((l, i) => new TextLine(l, x, first + i * lh)).ToList();
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
        List<IBox> boxes = d.Nodes.Cast<IBox>().Concat(d.TextBoxes).Concat(d.Groups).ToList();
        if (boxes.Count == 0) return null;
        double minX = boxes.Min(b => b.X), minY = boxes.Min(b => b.Y);
        double maxX = boxes.Max(b => b.X + b.W), maxY = boxes.Max(b => b.Y + b.H + ExtraBelow(b));
        // Labels under circles/outcomes and descriptions can be wider than the shape.
        foreach (Node n in d.Nodes)
        {
            if (OutsideTextBox(n, Layout(n)) is not { } r) continue;
            minX = Math.Min(minX, r.X);
            maxX = Math.Max(maxX, r.X + r.W);
        }
        return (minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>Bounds that wrap a set of boxes plus padding, with room for a title bar at the top (a new group).</summary>
    public static (double X, double Y, double W, double H) GroupBounds(IEnumerable<IBox> boxes) =>
        GroupBounds(boxes, Group.Header, 0);

    /// <summary>Bounds that wrap a set of boxes plus padding, with room for this group's title where it sits inside.</summary>
    public static (double X, double Y, double W, double H) GroupBounds(IEnumerable<IBox> boxes, Group g) =>
        GroupBounds(boxes, GroupInsetTop(g), GroupInsetBottom(g));

    static (double X, double Y, double W, double H) GroupBounds(IEnumerable<IBox> boxes, double insetTop, double insetBottom)
    {
        List<IBox> list = boxes.ToList();
        double minX = list.Min(b => b.X), minY = list.Min(b => b.Y);
        double maxX = list.Max(b => b.X + b.W), maxY = list.Max(b => b.Y + b.H + ExtraBelow(b));
        return (minX - Group.Pad, minY - Group.Pad - insetTop, maxX - minX + Group.Pad * 2, maxY - minY + Group.Pad * 2 + insetTop + insetBottom);
    }

    // ───────────── Group title (top / bottom, band / plain text) ─────────────

    static bool TitleBelowBox(Group g) => g.HeaderPos == GroupLook.Bottom && g.HeaderStyle == GroupLook.Text;

    /// <summary>Space the title takes inside the top of the group.</summary>
    public static double GroupInsetTop(Group g) => g.HeaderPos == GroupLook.Bottom ? 0 : Group.Header;

    /// <summary>Space the title takes inside the bottom of the group (a band at the bottom).</summary>
    public static double GroupInsetBottom(Group g) => g.HeaderPos == GroupLook.Bottom && g.HeaderStyle == GroupLook.Band ? Group.Header : 0;

    /// <summary>The title area: the band (top or bottom), the top strip for a plain title, or the strip under the box.
    /// Used to draw the band, as the drag handle, and for the inline title editor.</summary>
    public static (double X, double Y, double W, double H) GroupTitleArea(Group g)
    {
        if (TitleBelowBox(g)) return (g.X, g.Y + g.H + 2, g.W, Group.Header - 4);
        if (g.HeaderPos == GroupLook.Bottom) return (g.X, g.Y + g.H - Group.Header, g.W, Group.Header);
        return (g.X, g.Y, g.W, Group.Header);
    }

    /// <summary>Title baseline and anchor: left in a band or top strip, centred under the box.</summary>
    public static (double X, double Y, string Anchor) GroupTitlePos(Group g)
    {
        (double x, double y, double w, double h) = GroupTitleArea(g);
        return TitleBelowBox(g) ? (x + w / 2, y + 17, "middle") : (x + 14, y + 21, "start");
    }

    /// <summary>The tinted band behind the title (rounded on the outside corners), or null for a plain-text title.</summary>
    public static string? GroupBandPath(Group g)
    {
        if (g.HeaderStyle != GroupLook.Band) return null;
        double x = g.X, w = g.W, r = 10;
        if (g.HeaderPos == GroupLook.Bottom)
        {
            double y0 = g.Y + g.H - Group.Header, y1 = g.Y + g.H;
            return $"M{F(x)},{F(y0)} V{F(y1 - r)} Q{F(x)},{F(y1)} {F(x + r)},{F(y1)} H{F(x + w - r)} Q{F(x + w)},{F(y1)} {F(x + w)},{F(y1 - r)} V{F(y0)} Z";
        }
        double y = g.Y;
        return $"M{F(x)},{F(y + Group.Header)} V{F(y + r)} Q{F(x)},{F(y)} {F(x + r)},{F(y)} H{F(x + w - r)} Q{F(x + w)},{F(y)} {F(x + w)},{F(y + r)} V{F(y + Group.Header)} Z";
    }

    /// <summary>Fill opacity of the group body (0 = no fill; the body stays clickable).</summary>
    public static double GroupFillOpacity(Group g) => g.Filled ? 0.6 : 0;

    /// <summary>
    /// Space taken under a box by text drawn outside it: the label under a circle/outcome and the description.
    /// A one-line label under a circle takes 34px (as before descriptions existed); more lines take more.
    /// </summary>
    public static double ExtraBelow(IBox b)
    {
        if (b is Group g) return TitleBelowBox(g) ? Group.Header : 0;
        if (b is not Node n || (!LabelBelow(n) && n.Description is null)) return 0;
        LabelLayout lay = Layout(n);
        return OutsideTextBox(n, lay) is { } r ? r.Y + r.H - (n.Y + n.H) + 8 : 0;
    }

    /// <summary>Full connector geometry: path plus where (and at what angle) its label sits.</summary>
    public static EdgeGeom EdgeGeometry(IBox a, IBox b, string style, string labelOrient = LabelOrients.Horizontal, double labelPos = 0.5)
    {
        List<(double X, double Y)> pts = Route(a, b, style);
        (double x, double y, double seg) = PointAt(pts, labelPos);
        return new EdgeGeom(PathD(pts), x, y, LabelAngle(labelOrient, seg), seg);
    }

    /// <summary>Connector geometry for a stored edge, honouring pinned sides and a dragged bend.</summary>
    public static EdgeGeom EdgeGeometry(IBox a, IBox b, Edge e)
    {
        EdgeRoute r = Route(a, b, e);
        (double x, double y, double seg) = PointAt(r.Points, e.LabelPos);
        return new EdgeGeom(PathD(r.Points), x, y, LabelAngle(e.LabelOrient, seg), seg);
    }

    public static EdgeRoute Route(IBox a, IBox b, Edge e) =>
        Route(a, b, e.Style, Sides.IsValid(e.FromSide) ? e.FromSide : null, Sides.IsValid(e.ToSide) ? e.ToSide : null, e.Bend);

    public static List<(double X, double Y)> Route(IBox a, IBox b, string style) => Route(a, b, style, null, null, 0).Points;

    public static EdgeRoute Route(IBox a, IBox b, string style, string? fromSide, string? toSide, double bend)
    {
        if (style == EdgeStyles.Straight) return StraightRoute(a, b, fromSide, toSide);
        if (fromSide is null && toSide is null)
        {
            (string s1, string s2) = AutoSides(a, b);
            return ElbowRoute(a, b, s1, s2, bend, legacy: true);
        }
        if (fromSide is not null && toSide is not null) return ElbowRoute(a, b, fromSide, toSide, bend, legacy: false);

        // One end pinned: pick the other end's side that gives the shortest, simplest route.
        EdgeRoute? best = null;
        double bestScore = double.MaxValue;
        foreach (string s in Sides.All)
        {
            EdgeRoute r = ElbowRoute(a, b, fromSide ?? s, toSide ?? s, bend, legacy: false);
            double score = Length(r.Points) + 25 * r.Points.Count;
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
        (double X, double Y) aimA = p2 ?? cb;
        (double X, double Y) aimB = p1 ?? ca;
        (double X, double Y) start = p1 ?? Boundary(a, aimA.X, aimA.Y);
        (double X, double Y) end = p2 ?? Boundary(b, aimB.X, aimB.Y);
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
        (double X, double Y) p1 = SidePoint(a, s1);
        (double X, double Y) p2 = SidePoint(b, s2);
        (double X, double Y) d1 = Sides.Dir(s1);
        (double X, double Y) d2 = Sides.Dir(s2);
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
                (double X, double Y) m1 = Add(p1, d1, gap / 2);
                (double X, double Y) m2 = Add(p2, d2, gap / 2);
                pts = new() { p1, m1, m2, p2 };
                idx = 1;
            }
            else
            {
                // Target is behind the start side: step out, cross over between (or around) the shapes, step back in.
                (double X, double Y) n = (Math.Abs(d1.Y), Math.Abs(d1.X)); // perpendicular axis
                (double aMin, double aMax) = Span(a, n);
                (double bMin, double bMax) = Span(b, n);
                double lane = aMax < bMin ? (aMax + bMin) / 2
                            : bMax < aMin ? (bMax + aMin) / 2
                            : Math.Max(aMax, bMax) + Stub;
                (double X, double Y) q1 = Add(p1, d1, Stub);
                (double X, double Y) q2 = Add(p2, d2, Stub);
                double o1 = lane - Dot(q1, n), o2 = lane - Dot(q2, n);
                pts = new() { p1, q1, Add(q1, n, o1), Add(q2, n, o2), q2, p2 };
                idx = 2;
            }
        }
        else if (same)
        {
            // U: out past whichever end sticks out further, across, back in.
            double level = Math.Max(Dot(p1, d1), Dot(p2, d1)) + Stub;
            (double X, double Y) q1 = Add(p1, d1, level - Dot(p1, d1));
            (double X, double Y) q2 = Add(p2, d2, level - Dot(p2, d2));
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
                (double X, double Y) q1 = Add(p1, d1, out1);
                (double X, double Y) q2 = Add(p2, d2, Stub);
                (double X, double Y) x = Add(q1, d2, Dot(Sub(q2, q1), d2));
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
        if (Math.Abs(tx - cx) < 1e-6 && Math.Abs(ty - cy) < 1e-6) return (cx, cy);
        return OutlineOf(box).Boundary(box, tx, ty);
    }

    public static string PathD(List<(double X, double Y)> pts) =>
        "M" + string.Join(" L", pts.Select(p => $"{F(p.X)},{F(p.Y)}"));

    /// <summary>Point at fraction t of the polyline's length, plus that segment's angle in degrees.</summary>
    public static (double X, double Y, double Angle) PointAt(List<(double X, double Y)> pts, double t)
    {
        t = Math.Clamp(t, 0, 1);
        List<double> lengths = new List<double>();
        for (int i = 0; i < pts.Count - 1; i++)
            lengths.Add(Math.Sqrt(Math.Pow(pts[i + 1].X - pts[i].X, 2) + Math.Pow(pts[i + 1].Y - pts[i].Y, 2)));
        double total = lengths.Sum();
        if (total < 1e-6) return (pts[0].X, pts[0].Y, 0);
        double target = total * t;
        for (int i = 0; i < lengths.Count; i++)
        {
            if (target <= lengths[i] || i == lengths.Count - 1)
            {
                double f = lengths[i] < 1e-6 ? 0 : Math.Min(1, target / lengths[i]);
                (double ax, double ay) = pts[i]; (double bx, double by) = pts[i + 1];
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

    // ───────────── Connector labels ─────────────

    public const int MaxEdgeLabelLines = 2;
    public const double EdgeLabelLineHeight = 15;
    /// <summary>Clear space between the connector and text set beside it.</summary>
    const double EdgeLabelGap = 4;

    /// <summary>A label's lines: at most two (anything after the second line break joins line two).</summary>
    public static List<string> EdgeLabelLines(string? label)
    {
        List<string> parts = (label ?? "").Replace("\r", "").Split('\n').Select(l => l.Trim()).ToList();
        while (parts.Count > 0 && parts[^1].Length == 0) parts.RemoveAt(parts.Count - 1);
        while (parts.Count > 0 && parts[0].Length == 0) parts.RemoveAt(0);
        if (parts.Count > MaxEdgeLabelLines)
            parts = new List<string> { parts[0], string.Join(" ", parts.Skip(1).Where(l => l.Length > 0)) };
        return parts;
    }

    /// <summary>The label text kept to two lines (used when editing finishes or text is pasted).</summary>
    public static string ClampEdgeLabel(string? label)
    {
        string raw = (label ?? "").Replace("\r", "");
        string[] parts = raw.Split('\n');
        if (parts.Length <= MaxEdgeLabelLines) return raw;
        return parts[0] + "\n" + string.Join(" ", parts.Skip(1).Select(l => l.Trim()).Where(l => l.Length > 0));
    }

    /// <summary>
    /// Where a connector label's lines go. When the text runs along the line, "above"/"below" stack the text on that
    /// side and a two-line inline label straddles the line (line one above, line two below). When the text runs
    /// across the line (level text on an up-down segment, or 90° labels), inline sits on the line in a box and
    /// above/below put it to the left/right of the line as the text reads.
    /// </summary>
    public static EdgeLabelLayout LayoutEdgeLabel(Edge e, EdgeGeom geo)
    {
        List<string> lines = EdgeLabelLines(e.Label);
        if (lines.Count == 0) lines.Add("");
        int n = lines.Count;
        double lh = EdgeLabelLineHeight;
        double textW = lines.Max(l => l.Length * 7.0);
        double d = (geo.LabelAngle - geo.SegAngle) * Math.PI / 180;
        bool along = Math.Abs(Math.Cos(d)) >= 0.5;
        string place = e.LabelPlace is LabelPlaces.Above or LabelPlaces.Below ? e.LabelPlace : LabelPlaces.Inline;
        // Baselines: a 12px line's visual middle is ~4px above its baseline; cap height ~9, descent ~3.
        const double Ascent = 9, Descent = 3, Mid = 4;
        List<EdgeLabelLine> result = new List<EdgeLabelLine>();
        string anchor = "middle";
        Rect? box = null;
        double x = 0;
        double[] ys;

        if (along && place == LabelPlaces.Inline && n == 2)
            ys = new[] { -EdgeLabelGap - Descent, EdgeLabelGap + Ascent };
        else if (along && place == LabelPlaces.Above)
            ys = Enumerable.Range(0, n).Select(i => -EdgeLabelGap - Descent - (n - 1 - i) * lh).ToArray();
        else if (along && place == LabelPlaces.Below)
            ys = Enumerable.Range(0, n).Select(i => EdgeLabelGap + Ascent + i * lh).ToArray();
        else
        {
            // Centred on the point (inline single line, or text across the line).
            ys = Enumerable.Range(0, n).Select(i => Mid + (i - (n - 1) / 2.0) * lh).ToArray();
            if (!along && place == LabelPlaces.Above) { anchor = "end"; x = -EdgeLabelGap - 2; }
            else if (!along && place == LabelPlaces.Below) { anchor = "start"; x = EdgeLabelGap + 2; }
            else
            {
                double bw = textW + 12, bh = n * lh + 5;
                box = new Rect(-bw / 2, -bh / 2, bw, bh);
            }
        }
        for (int i = 0; i < n; i++) result.Add(new EdgeLabelLine(lines[i], x, ys[i]));

        double left = anchor switch { "end" => x - textW, "start" => x, _ => x - textW / 2 };
        Rect hit = box ?? new Rect(left - 3, ys[0] - Ascent - 3, textW + 6, ys[^1] - ys[0] + Ascent + Descent + 6);
        return new EdgeLabelLayout(result, anchor, box, box is null, hit);
    }

    /// <summary>Approximate label box width for a single-line label.</summary>
    public static double LabelWidth(string text) => (text ?? "").Length * 7 + 12;

    /// <summary>Very simple word wrap for SVG text, based on an average character width.</summary>
    public static List<string> Wrap(string text, double width, double charWidth = 7.2)
    {
        int maxChars = Math.Max(4, (int)(width / charWidth));
        List<string> lines = new List<string>();
        foreach (string para in (text ?? "").Replace("\r", "").Split('\n'))
        {
            StringBuilder sb = new StringBuilder();
            foreach (string word in para.Split(' '))
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
