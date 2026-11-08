namespace BeDiagram.Models.Catalogue;

/// <summary>Connection points on a shape: the middle of each side.</summary>
public static class Sides
{
    public const string Top = "top", Right = "right", Bottom = "bottom", Left = "left";
    public static readonly string[] All = { Top, Right, Bottom, Left };

    /// <summary>The sides a row of a list / IPO node has ports on.</summary>
    public static readonly string[] LeftRight = { Right, Left };

    /// <summary>Outward direction of a side.</summary>
    public static (double X, double Y) Dir(string side) => side switch
    {
        Top => (0, -1), Right => (1, 0), Bottom => (0, 1), _ => (-1, 0),
    };

    public static string Opposite(string side) => side switch
    {
        Top => Bottom, Right => Left, Bottom => Top, _ => Right,
    };

    public static bool IsValid(string? side) => side is Top or Right or Bottom or Left;
}
