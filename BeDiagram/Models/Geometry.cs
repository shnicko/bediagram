using System.Globalization;
using System.Text;

namespace BeDiagram.Models;

/// <summary>Shape and connector geometry shared by the canvas and the SVG exporter.</summary>
public static class Geometry
{
    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    public static string ShapePath(Node n)
    {
        var t = NodeTypes.Get(n.Type);
        double x = n.X, y = n.Y, w = n.W, h = n.H;
        switch (t.Shape)
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
            case "rect-rounded":
                return RoundRect(x, y, w, h, 14);
            default: // rect, check
                return RoundRect(x, y, w, h, 5);
        }
    }

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

    /// <summary>Bounding box of everything on the canvas (null when empty).</summary>
    public static (double X, double Y, double W, double H)? ContentBounds(Diagram d)
    {
        var boxes = d.Nodes.Cast<IBox>().Concat(d.Comments).Concat(d.Groups).ToList();
        if (boxes.Count == 0) return null;
        double minX = boxes.Min(b => b.X), minY = boxes.Min(b => b.Y);
        double maxX = boxes.Max(b => b.X + b.W), maxY = boxes.Max(b => b.Y + b.H + ExtraBelow(b));
        // Labels drawn under circles can be wider than the circle.
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
        b is Node n && NodeTypes.Get(n.Type).Shape == "circle" && Icons.Exists(n.Icon) ? 34 : 0;

    public record EdgeGeom(string D, double LabelX, double LabelY, double LabelAngle);

    /// <summary>Full connector geometry: path plus where (and at what angle) its label sits.</summary>
    public static EdgeGeom EdgeGeometry(IBox a, IBox b, string style, string labelOrient = LabelOrients.Horizontal, double labelPos = 0.5)
    {
        var pts = Route(a, b, style);
        var (x, y, seg) = PointAt(pts, labelPos);
        return new EdgeGeom(PathD(pts), x, y, LabelAngle(labelOrient, seg));
    }

    public static List<(double X, double Y)> Route(IBox a, IBox b, string style) =>
        style == EdgeStyles.Straight ? StraightRoute(a, b) : ElbowRoute(a, b);

    /// <summary>Straight line between the shapes' outlines, aimed centre to centre.</summary>
    static List<(double X, double Y)> StraightRoute(IBox a, IBox b)
    {
        double acx = a.X + a.W / 2, acy = a.Y + a.H / 2, bcx = b.X + b.W / 2, bcy = b.Y + b.H / 2;
        return new() { Boundary(a, bcx, bcy), Boundary(b, acx, acy) };
    }

    /// <summary>Right-angled connector between side mid-points.</summary>
    static List<(double X, double Y)> ElbowRoute(IBox a, IBox b)
    {
        double acx = a.X + a.W / 2, acy = a.Y + a.H / 2;
        double bcx = b.X + b.W / 2, bcy = b.Y + b.H / 2;
        double dx = bcx - acx, dy = bcy - acy;

        // Vertical flow if the target is clearly above/below, otherwise horizontal.
        bool vertical = Math.Abs(dy) - (a.H + b.H) / 2 > Math.Abs(dx) - (a.W + b.W) / 2 - 20
                        && Math.Abs(dy) > (a.H + b.H) / 2;
        if (vertical)
        {
            double x1 = acx, y1 = dy > 0 ? a.Y + a.H : a.Y;
            double x2 = bcx, y2 = dy > 0 ? b.Y : b.Y + b.H;
            double my = (y1 + y2) / 2;
            if (Math.Abs(x1 - x2) < 1) return new() { (x1, y1), (x1, y2) };
            return new() { (x1, y1), (x1, my), (x2, my), (x2, y2) };
        }
        else
        {
            double y1 = acy, x1 = dx > 0 ? a.X + a.W : a.X;
            double y2 = bcy, x2 = dx > 0 ? b.X : b.X + b.W;
            double mx = (x1 + x2) / 2;
            if (Math.Abs(y1 - y2) < 1) return new() { (x1, y1), (x2, y1) };
            return new() { (x1, y1), (mx, y1), (mx, y2), (x2, y2) };
        }
    }

    /// <summary>Where a ray from the box centre towards (tx, ty) leaves the shape's outline.</summary>
    public static (double X, double Y) Boundary(IBox box, double tx, double ty)
    {
        double cx = box.X + box.W / 2, cy = box.Y + box.H / 2;
        double dx = tx - cx, dy = ty - cy;
        if (Math.Abs(dx) < 1e-6 && Math.Abs(dy) < 1e-6) return (cx, cy);
        var shape = box is Node n ? NodeTypes.Get(n.Type).Shape : "rect";

        if (shape == "circle")
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

    static List<(double, double)> Outline(IBox b, string shape)
    {
        double x = b.X, y = b.Y, w = b.W, h = b.H;
        switch (shape)
        {
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
