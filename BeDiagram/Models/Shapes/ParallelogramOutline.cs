namespace BeDiagram.Models.Shapes;

/// <summary>Data / IFC: a parallelogram slanting right.</summary>
public class ParallelogramOutline : ShapeOutline
{

    static double Slant(IBox b) => Math.Min(18, b.W / 6);

    public override string Path(IBox b)
    {
        double x = b.X, y = b.Y, w = b.W, h = b.H, k = Slant(b);
        return $"M{F(x + k)},{F(y)} H{F(x + w)} L{F(x + w - k)},{F(y + h)} H{F(x)} Z";
    }

    protected override List<(double X, double Y)> Polygon(IBox b)
    {
        double x = b.X, y = b.Y, w = b.W, h = b.H, k = Slant(b);
        return new() { (x + k, y), (x + w, y), (x + w - k, y + h), (x, y + h) };
    }

    public override (double X, double Y) LinkBadgeCentre(IBox b) => (b.X + b.W - Slant(b), b.Y + b.H);

    public override double IconInset => 22;
    public override string Mermaid(string label) => $"[/{label}/]";
}
