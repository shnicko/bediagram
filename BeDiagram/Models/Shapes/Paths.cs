using System.Globalization;

namespace BeDiagram.Models.Shapes;

/// <summary>SVG path data built from geometry: rounded rectangles and polylines. Shared by the outlines, connectors and
/// text boxes, so the canvas and every export draw the same paths.</summary>
public static class Paths
{
    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    public static string RoundRect(double x, double y, double w, double h, double r)
    {
        r = Math.Min(r, Math.Min(w, h) / 2);
        return $"M{F(x + r)},{F(y)} H{F(x + w - r)} Q{F(x + w)},{F(y)} {F(x + w)},{F(y + r)} V{F(y + h - r)} Q{F(x + w)},{F(y + h)} {F(x + w - r)},{F(y + h)} H{F(x + r)} Q{F(x)},{F(y + h)} {F(x)},{F(y + h - r)} V{F(y + r)} Q{F(x)},{F(y)} {F(x + r)},{F(y)} Z";
    }

    public static string PathD(List<(double X, double Y)> pts) =>
        "M" + string.Join(" L", pts.Select(p => $"{F(p.X)},{F(p.Y)}"));
}
