namespace BeDiagram.Models.Shapes;

/// <summary>Pass outcome: a scalloped seal.</summary>
public class SealOutline : ShapeOutline
{
    public override string Key => "seal";

    public override string Path(IBox b) => Geometry.SealPath(b.X + b.W / 2, b.Y + b.H / 2, Math.Min(b.W, b.H) / 2);

    public override (double X, double Y) Boundary(IBox b, double tx, double ty) => RadialBoundary(b, tx, ty);
    public override (double X, double Y) LinkBadgeCentre(IBox b) => RadialBadge(b);

    public override double GlyphScale => 0.44;
}
