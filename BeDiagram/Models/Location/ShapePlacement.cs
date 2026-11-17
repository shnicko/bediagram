namespace BeDiagram.Models.Location;

/// <summary>
/// A shape: X and Y are its centre (the middle of the circle or symbol, not of the text under it), so a new size
/// grows or shrinks it about the centre and connectors stay straight. The smallest size comes from its kind.
/// </summary>
public class ShapePlacement : Placement
{
    public override string SetOutName
    {
        get
        {
            return "centre";
        }
    }

    public override (double X, double Y) SetOut(IBox b)
    {
        return (b.X + b.W / 2, b.Y + b.H / 2);
    }

    public override (double X, double Y) TopLeftFor(double setX, double setY, double w, double h)
    {
        return (setX - w / 2, setY - h / 2);
    }

    public override (double W, double H) MinSize(IBox b)
    {
        // Only nodes get this placement (from their kind), and the kind knows the node's smallest size.
        Node n = (Node)b;
        return n.Kind.MinSize(n);
    }
}
