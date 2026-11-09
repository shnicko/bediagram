namespace BeDiagram.Models.Shapes;

/// <summary>Hold outcome: a rounded square centred in the box.</summary>
public class RoundedSquareOutline : ShapeOutline
{

    public override string Path(IBox b)
    {
        double s = Math.Min(b.W, b.H), sx = b.X + (b.W - s) / 2, sy = b.Y + (b.H - s) / 2;
        return Geometry.RoundRect(sx, sy, s, s, s * 0.22);
    }

    protected override List<(double X, double Y)> Polygon(IBox b)
    {
        double x = b.X, y = b.Y, w = b.W, h = b.H, s = Math.Min(w, h);
        return new() { (x + (w - s) / 2, y + (h - s) / 2), (x + (w + s) / 2, y + (h - s) / 2), (x + (w + s) / 2, y + (h + s) / 2), (x + (w - s) / 2, y + (h + s) / 2) };
    }

    public override (double X, double Y) LinkBadgeCentre(IBox b)
    {
        double s = Math.Min(b.W, b.H), k = s * 0.22 * (1 - D45);
        return (b.X + b.W / 2 + s / 2 - k, b.Y + b.H / 2 + s / 2 - k);
    }
}
