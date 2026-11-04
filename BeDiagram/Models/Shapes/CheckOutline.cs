namespace BeDiagram.Models.Shapes;

/// <summary>Rule check: a rectangle with a coloured stripe down the left edge (the icon sits clear of it).</summary>
public class CheckOutline : RectOutline
{
    public override string Key => "check";

    public override bool HasSideStripe => true;
    public override double IconInset => 18;
}
