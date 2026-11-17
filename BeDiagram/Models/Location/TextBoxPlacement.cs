namespace BeDiagram.Models.Location;

/// <summary>
/// A text box: X and Y are its <em>top centre</em>, because text boxes on Nick's diagrams mostly sit centred under or
/// over shapes. A new width grows or shrinks it evenly about that point and a new height keeps the top line; dragging
/// snaps the top centre to the grid. Not smaller than 40 × 24.
/// </summary>
public class TextBoxPlacement : Placement
{
    public override string SetOutName
    {
        get
        {
            return "top centre";
        }
    }

    public override (double X, double Y) SetOut(IBox b)
    {
        return (b.X + b.W / 2, b.Y);
    }

    public override (double X, double Y) TopLeftFor(double setX, double setY, double w, double h)
    {
        return (setX - w / 2, setY);
    }

    public override (double W, double H) MinSize(IBox b)
    {
        return (40, 24);
    }

    public override (double X, double Y) SnapPoint(IBox b, double x, double y)
    {
        return (x + b.W / 2, y);
    }

    /// <summary>Snaps the top centre: the middle across, the top line down.</summary>
    public override (double X, double Y) SnapTopLeft(IBox b, double x, double y, bool snapX, bool snapY, Func<double, double> snap)
    {
        if (snapX)
        {
            x = snap(x + b.W / 2) - b.W / 2;
        }
        if (snapY)
        {
            y = snap(y);
        }
        return (x, y);
    }
}
