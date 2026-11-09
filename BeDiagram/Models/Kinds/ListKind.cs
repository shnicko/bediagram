namespace BeDiagram.Models.Kinds;

/// <summary>A list: a title band over a stack of rows.</summary>
public class ListKind : PanelKind
{
    public override string? PaletteInnerPath => "M0,8 H48 M5,12 H43 V17 H5 Z M5,20 H43 V25 H5 Z";

    /// <summary>Three rows to start with.</summary>
    public override List<ListItem> NewItems() =>
        new List<ListItem> { new() { Label = "Item 1" }, new() { Label = "Item 2" }, new() { Label = "Item 3" } };
}
