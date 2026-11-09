namespace BeDiagram.Models.Catalogue;

/// <summary>Connection points on a shape: the middle of each side.</summary>
public static class Sides
{
    public static readonly Side[] All = { Side.Top, Side.Right, Side.Bottom, Side.Left };

    /// <summary>The sides a row of a list / IPO node has ports on.</summary>
    public static readonly Side[] LeftRight = { Side.Right, Side.Left };

    /// <summary>Outward direction of a side.</summary>
    public static (double X, double Y) Dir(Side side) => side switch
    {
        Side.Top => (0, -1), Side.Right => (1, 0), Side.Bottom => (0, 1), _ => (-1, 0),
    };
}
