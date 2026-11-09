namespace BeDiagram.Models.Shapes;

/// <summary>Conditional outcome: a warning triangle. Its glyph sits lower, at the visual centre.</summary>
public class TriangleOutline : ShapeOutline
{

    public override string Path(IBox b) => Geometry.PathD(Polygon(b)) + " Z";

    protected override List<(double X, double Y)> Polygon(IBox b) =>
        new() { (b.X + b.W / 2, b.Y), (b.X + b.W, b.Y + b.H), (b.X, b.Y + b.H) };

    public override (double X, double Y) LinkBadgeCentre(IBox b) => (b.X + b.W - 8, b.Y + b.H);

    public override double GlyphScale => 0.38;
    public override double GlyphCentreY => 0.62;
}
