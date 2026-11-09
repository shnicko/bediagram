namespace BeDiagram.Models.Shapes;

/// <summary>Fail outcome: a regular octagon (stop sign) centred in the box, flat sides touching it.</summary>
public class OctagonOutline : ShapeOutline
{

    public override string Path(IBox b) => Geometry.PathD(Polygon(b)) + " Z";

    protected override List<(double X, double Y)> Polygon(IBox b)
    {
        double r = Math.Min(b.W, b.H) / 2, k = r * Math.Tan(Math.PI / 8);
        double cx = b.X + b.W / 2, cy = b.Y + b.H / 2;
        return new() { (cx - k, cy - r), (cx + k, cy - r), (cx + r, cy - k), (cx + r, cy + k),
                       (cx + k, cy + r), (cx - k, cy + r), (cx - r, cy + k), (cx - r, cy - k) };
    }

    public override (double X, double Y) LinkBadgeCentre(IBox b) => RadialBadge(b);
}
