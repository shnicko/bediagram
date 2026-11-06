namespace BeDiagram.Models.Emphasis;

/// <summary>The emphasis levels (strategies) and the lookup from a node's stored key.</summary>
public static class Emphases
{
    public const string Major = "major", Normal = "normal", Minor = "minor";

    /// <summary>In panel order: Major, Normal, Minor.</summary>
    public static readonly IReadOnlyList<IEmphasis> All = new List<IEmphasis> { new MajorEmphasis(), new NormalEmphasis(), new MinorEmphasis() };

    /// <summary>The emphasis for a stored key; anything unknown (or missing) is normal.</summary>
    public static IEmphasis Get(string? key) => All.FirstOrDefault(e => e.Key == key) ?? All[1];

    /// <summary>The emphasis a node is drawn with.</summary>
    public static IEmphasis Of(Node n) => Get(n.Emphasis);
}
