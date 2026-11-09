namespace BeDiagram.Models.Shapes;

/// <summary>Plain process box: a rectangle with slightly rounded corners.</summary>
public class RectOutline : ShapeOutline
{

    public override string Path(IBox b) => Geometry.RoundRect(b.X, b.Y, b.W, b.H, 5);
}
