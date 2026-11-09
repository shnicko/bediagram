namespace BeDiagram.Models.Shapes;

/// <summary>Action: a rectangle with well-rounded corners.</summary>
public class RoundedRectOutline : ShapeOutline
{

    public override string Path(IBox b) => Geometry.RoundRect(b.X, b.Y, b.W, b.H, 14);

    public override (double X, double Y) LinkBadgeCentre(IBox b) =>
        (b.X + b.W - 14 * (1 - D45), b.Y + b.H - 14 * (1 - D45));

    public override string Mermaid(string label) => $"({label})";
}
