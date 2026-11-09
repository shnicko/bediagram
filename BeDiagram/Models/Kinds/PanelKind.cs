using BeDiagram.Models.Layout;

namespace BeDiagram.Models.Kinds;

/// <summary>
/// List and input–process–output nodes ("panels"): a titled box holding rows (<see cref="Node.Items"/>), laid out by
/// <see cref="PanelGeometry"/>. The node's label is the title in the band at the top.
/// </summary>
public abstract class PanelKind : NodeKind
{
    public override bool IsPanel => true;

    public override LabelLayout LayoutLabel(Node n, string label) => PanelGeometry.HeaderLabel(n, label);

    public abstract override List<ListItem> NewItems();

    /// <summary>A new panel gets its starter rows, and is exactly as tall as they need (rows have a fixed height).</summary>
    public override void Init(Node n, string? variant)
    {
        n.Items = NewItems();
        n.H = PanelGeometry.NeededHeight(n);
    }
}
