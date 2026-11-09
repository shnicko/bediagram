using BeDiagram.Models.Panels;

namespace BeDiagram.Models.Kinds;

/// <summary>A list: a title band over a stack of rows.</summary>
public class ListKind : PanelKind
{
    static readonly IPanelLayout Layout = new ListLayout();

    public override string? PaletteInnerPath => "M0,8 H48 M5,12 H43 V17 H5 Z M5,20 H43 V25 H5 Z";

    public override IPanelLayout LayoutOf(Node n) => Layout;

    /// <summary>Three rows to start with.</summary>
    public override List<ListItem> NewItems() =>
        new List<ListItem> { new() { Label = "Item 1" }, new() { Label = "Item 2" }, new() { Label = "Item 3" } };

    /// <summary>A new row "Item N" at the end.</summary>
    public override ListItem AddRow(Node n, string? section)
    {
        n.Items ??= new List<ListItem>();
        ListItem item = new ListItem { Label = $"Item {n.Items.Count + 1}" };
        n.Items.Add(item);
        return item;
    }

    public override List<ListItem> Peers(Node n, string itemId) => n.Items ?? new List<ListItem>();

    /// <summary>A list has no sections.</summary>
    public override void TidyRows(Node n)
    {
        n.Items ??= NewItems();
        foreach (ListItem i in n.Items) i.Section = null;
    }
}
