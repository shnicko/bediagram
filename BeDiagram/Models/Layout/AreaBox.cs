using BeDiagram.Models.Location;

namespace BeDiagram.Models.Layout;

/// <summary>A plain box with no item behind it, for drawing an outline at another size (e.g. a major step's halo ring).</summary>
public class AreaBox : IBox
{
    public AreaBox(double x, double y, double w, double h)
    {
        X = x;
        Y = y;
        W = w;
        H = h;
    }

    public string Id
    {
        get
        {
            return "";
        }
    }

    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }

    /// <summary>Placed by its top-left corner (it's never moved or sized by hand).</summary>
    public IPlacement Placement
    {
        get
        {
            return Placements.Corner;
        }
    }
}
