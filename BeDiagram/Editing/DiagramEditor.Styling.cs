using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Kinds;

namespace BeDiagram.Editing;

/// <summary>Shape and group looks: type, outcome, round, colour, fill, emphasis, icons, group styles, bulk restyle.</summary>
public partial class DiagramEditor
{
    // ───────────── Shapes and groups ─────────────

    public void ChangeType(Node n, NodeType? type)
    {
        if (type is null || type == n.Type) return;
        Change();
        NodeTypeInfo oldT = NodeTypes.Get(n.Type);
        NodeTypeInfo t = NodeTypes.Get(type.Value);
        // A label still showing the old type's (or outcome's) name follows the new type.
        bool namedForType = n.Label == n.Kind.NameOf(n);
        double cx = n.X + n.W / 2; double cy = n.Y + n.H / 2;
        n.Type = t.Key;
        INodeKind kind = n.Kind;
        // A circle-drawn shape stays a circle when the new type can be one, and its type's icon follows the type.
        bool round = n.Round && kind.CanBeRound;
        if (n.Round && n.Icon == oldT.RoundIcon) n.Icon = round ? t.RoundIcon : t.Icon;
        n.Round = round;
        kind.Adopt(n);
        if (namedForType) n.Label = kind.RetypeLabel(n);
        (double w, double h) = t.SizeFor(round);
        n.W = w; n.H = h;
        n.X = SnapV(cx) - w / 2; n.Y = SnapV(cy) - h / 2;
        foreach ((string k, string v) in t.NewProperties()) n.Properties.TryAdd(k, v);
        RetypeRows(n);
        // Becoming an outcome drops the number (and closes the gap); leaving one goes on the end of the flow.
        Steps.Normalise();
    }

    /// <summary>Description or text width edited: the text under the shape changed size, so its group may need to grow.</summary>
    public void OutsideTextChanged(Node n)
    {
        MarkDirty();
        if (FindGroup(n.GroupId) is { } g) GrowGroup(g);
    }

    public void SetClearTextBack(Node n, bool clear) { Change(); n.ClearTextBack = clear; }

    public void SetOutcome(Node n, OutcomeType outcome)
    {
        if (n.Outcome == outcome) return;
        Change();
        // A label still showing the old outcome's name follows the new one.
        if (n.Label == Outcomes.Get(n.Outcome).Display) n.Label = Outcomes.Get(outcome).Display;
        n.Outcome = outcome;
    }

    public void SetIcon(Node n, string? icon) { Change(); n.Icon = icon; }

    /// <summary>
    /// Draws a classic shape as a circle (icon in the middle, label underneath) or back as its own shape, keeping its
    /// centre, label, colour and step. Going round, a shape with no icon gets its type's circle icon; going back, that
    /// icon is swapped for the type's usual one (none for most shapes) so it doesn't sit beside the label.
    /// </summary>
    public void SetRound(Node n, bool round)
    {
        if (!n.Kind.CanBeRound || n.Round == round) return;
        Change();
        double cx = n.X + n.W / 2, cy = n.Y + n.H / 2;
        NodeTypeInfo t = NodeTypes.Get(n.Type);
        n.Round = round;
        if (round) n.Icon ??= t.RoundIcon;
        else if (n.Icon == t.RoundIcon) n.Icon = t.Icon;
        (n.W, n.H) = t.SizeFor(round);
        n.X = cx - n.W / 2;
        n.Y = cy - n.H / 2;
        GrowAround(n);
    }

    /// <summary>Major / normal / minor look (see <see cref="Emphases"/>).</summary>
    /// <summary>Sets the emphasis of every shape that doesn't have it yet, in one undo step.</summary>
    public void SetEmphasis(IReadOnlyList<Node> nodes, EmphasisLevel level)
    {
        List<Node> todo = nodes.Where(n => n.Emphasis != level).ToList();
        if (todo.Count == 0) return;
        Change();
        foreach (Node n in todo) n.Emphasis = level;
    }

    // ───────────── Restyling one item or many ─────────────

    /// <summary>
    /// Applies a look change (colour, fill, border, font, …) to every item in one undo step. The style controls in the
    /// panel use this for a single item and for a whole selection alike.
    /// </summary>
    public void Restyle<T>(IReadOnlyList<T> items, Action<T> change)
    {
        if (items.Count == 0) return;
        Change();
        foreach (T item in items) change(item);
    }

    public void SetNodeLink(Node n, string? link) { Change(); n.Link = link; }

    public void SetGroupLink(Group g, string? link) { Change(); g.Link = link; }
}
