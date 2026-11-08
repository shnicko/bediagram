namespace BeDiagram.Models.Shapes;

/// <summary>List and input–process–output nodes: a box with rounded corners and a title band (see <see cref="PanelGeometry"/>).</summary>
public class PanelOutline : ShapeOutline
{
    public const double Radius = 12;

    public override string Key => "panel";

    public override string Path(IBox b) => Geometry.RoundRect(b.X, b.Y, b.W, b.H, Radius);

    public override (double X, double Y) LinkBadgeCentre(IBox b) =>
        (b.X + b.W - Radius * (1 - D45), b.Y + b.H - Radius * (1 - D45));
}
