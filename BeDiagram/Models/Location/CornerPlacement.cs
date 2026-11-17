namespace BeDiagram.Models.Location;

/// <summary>
/// Placed by the top-left corner: groups (<see cref="GroupPlacement"/>) and the plain boxes used for drawing
/// (<see cref="Layout.AreaBox"/>, <see cref="Layout.ItemBox"/>). X and Y are the top-left corner, and a new size keeps
/// it. Not smaller than 40 × 24.
/// </summary>
public class CornerPlacement : Placement
{
    public override string SetOutName
    {
        get
        {
            return "top-left";
        }
    }

    public override (double X, double Y) SetOut(IBox b)
    {
        return (b.X, b.Y);
    }

    public override (double X, double Y) TopLeftFor(double setX, double setY, double w, double h)
    {
        return (setX, setY);
    }

    public override (double W, double H) MinSize(IBox b)
    {
        return (40, 24);
    }
}
