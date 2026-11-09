using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models;

/// <summary>
/// Factory for new shapes from a palette key. What a fresh node of each type starts with (size, icon, default
/// properties) is data on its <see cref="NodeTypeInfo"/>; what depends on its kind (label, outcome, rows) comes from
/// the type's <see cref="Kinds.INodeKind"/>.
/// </summary>
public static class NodeFactory
{
    /// <summary>
    /// A new node for a palette key (a node type, "outcome:&lt;variant&gt;" or "round:&lt;type&gt;"), centred on
    /// (<paramref name="cx"/>, <paramref name="cy"/>). The caller snaps the centre and adds the node to the diagram.
    /// </summary>
    public static Node Create(string paletteKey, double cx, double cy)
    {
        (string type, string? variant) = ParsePaletteKey(paletteKey);
        NodeTypeInfo t = NodeTypes.Get(type);
        bool round = IsRoundKey(paletteKey) && t.Kind.CanBeRound;
        (double w, double h) = t.SizeFor(round);
        Node n = new Node
        {
            Type = t.Key,
            Round = round,
            Icon = round ? t.RoundIcon : t.Icon,
            W = w,
            H = h,
            X = cx - w / 2,
            Properties = t.NewProperties(),
        };
        t.Kind.Init(n, variant);
        n.Label = t.Kind.NewLabel(n);
        // After Init: a list / IPO node is as tall as its rows need.
        n.Y = cy - n.H / 2;
        return n;
    }

    /// <summary>Palette key prefix for a classic shape drawn as a circle, e.g. "round:decision".</summary>
    public const string RoundPrefix = "round:";

    public static bool IsRoundKey(string key) => key.StartsWith(RoundPrefix);

    /// <summary>Width and height a new node from this palette key gets (before rows are counted for lists / IPO boxes).</summary>
    public static (double W, double H) SizeFor(string paletteKey)
    {
        NodeTypeInfo t = NodeTypes.Get(ParsePaletteKey(paletteKey).Type);
        return t.SizeFor(IsRoundKey(paletteKey) && t.Kind.CanBeRound);
    }

    /// <summary>Palette keys are a node type, "outcome:&lt;variant&gt;" for an outcome, or "round:&lt;type&gt;" for a shape drawn as a circle.</summary>
    public static (string Type, string? Outcome) ParsePaletteKey(string key) =>
        key.StartsWith(NodeTypes.Outcome + ":") ? (NodeTypes.Outcome, key[(NodeTypes.Outcome.Length + 1)..])
        : IsRoundKey(key) ? (key[RoundPrefix.Length..], null)
        : (key, null);
}
