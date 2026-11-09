using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models;

/// <summary>
/// Factory for new shapes from a <see cref="PaletteItem"/>. What a fresh node of each type starts with (size, icon, default
/// properties) is data on its <see cref="NodeTypeInfo"/>; what depends on its kind (label, outcome, rows) comes from
/// the type's <see cref="Kinds.INodeKind"/>.
/// </summary>
public static class NodeFactory
{
    /// <summary>
    /// A new node for a palette item (a type, an outcome tile, or a classic shape as a circle), centred on
    /// (<paramref name="cx"/>, <paramref name="cy"/>). The caller snaps the centre and adds the node to the diagram.
    /// </summary>
    public static Node Create(PaletteItem item, double cx, double cy)
    {
        NodeTypeInfo t = NodeTypes.Get(item.Type);
        bool round = item.Round && t.Kind.CanBeRound;
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
        t.Kind.Init(n, item.Outcome);
        n.Label = t.Kind.NewLabel(n);
        // After Init: a list / IPO node is as tall as its rows need.
        n.Y = cy - n.H / 2;
        return n;
    }

    /// <summary>Width and height a new node from this palette item gets (before rows are counted for lists / IPO boxes).</summary>
    public static (double W, double H) SizeFor(PaletteItem item)
    {
        NodeTypeInfo t = NodeTypes.Get(item.Type);
        return t.SizeFor(item.Round && t.Kind.CanBeRound);
    }
}
