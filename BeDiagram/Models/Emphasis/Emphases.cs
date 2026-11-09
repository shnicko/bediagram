namespace BeDiagram.Models.Emphasis;

/// <summary>The emphasis levels (strategies) and the lookup from a node's level.</summary>
public static class Emphases
{
    /// <summary>In panel order: Major, Normal, Minor.</summary>
    public static readonly IReadOnlyList<IEmphasis> All = new List<IEmphasis> { new MajorEmphasis(), new NormalEmphasis(), new MinorEmphasis() };

    /// <summary>The strategy for a level.</summary>
    public static IEmphasis Get(EmphasisLevel level) => All.First(e => e.Level == level);

    /// <summary>The emphasis a node is drawn with.</summary>
    public static IEmphasis Of(Node n) => Get(n.Emphasis);
}
