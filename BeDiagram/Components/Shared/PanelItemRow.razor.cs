using Microsoft.AspNetCore.Components;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Components.Shared;

/// <summary>One row of a list / IPO node in the side panel: icon, label, description, section (IPO), move and remove.</summary>
public partial class PanelItemRow
{
    [Parameter, EditorRequired] public DiagramEditor Editor { get; set; } = default!;
    [Parameter, EditorRequired] public Node Node { get; set; } = default!;
    [Parameter, EditorRequired] public ListItem Item { get; set; } = default!;

    /// <summary>True in an IPO box (shows the section picker).</summary>
    [Parameter] public bool Ipo { get; set; }

    /// <summary>The icon picker under this row is open.</summary>
    [Parameter] public bool IconOpen { get; set; }

    /// <summary>Asks the list to open or close this row's icon picker (only one is open at a time).</summary>
    [Parameter] public EventCallback<string> OnToggleIcon { get; set; }

    /// <summary>Called after every change, so the page redraws the canvas.</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    string filter = "";

    string IconTitle => Icons.Exists(Item.Icon) ? $"Icon: {Item.Icon} (click to change)" : "Add an icon";

    Task LabelTyped(ChangeEventArgs e)
    {
        Item.Label = e.Value?.ToString() ?? "";
        Editor.MarkDirty();
        return OnChanged.InvokeAsync();
    }

    Task DescTyped(ChangeEventArgs e)
    {
        Item.Description = e.Value?.ToString();
        Editor.MarkDirty();
        return OnChanged.InvokeAsync();
    }

    Task Up() { Editor.MoveItem(Node, Item.Id, -1); return OnChanged.InvokeAsync(); }

    Task Down() { Editor.MoveItem(Node, Item.Id, 1); return OnChanged.InvokeAsync(); }

    Task Remove() { Editor.RemoveItem(Node, Item.Id); return OnChanged.InvokeAsync(); }

    Task SectionPicked(ChangeEventArgs e) { Editor.SetItemSection(Node, Item.Id, EnumNames.Parse<IpoSection>(e.Value?.ToString())); return OnChanged.InvokeAsync(); }

    /// <summary>Option values carry the saved section names ("output").</summary>
    static string SectionName(IpoSection s) => EnumNames.Of(s);

    Task ToggleIcon() => OnToggleIcon.InvokeAsync(Item.Id);

    async Task PickIcon(string name)
    {
        Editor.SetItemIcon(Node, Item, name);
        await OnToggleIcon.InvokeAsync(Item.Id);
        await OnChanged.InvokeAsync();
    }

    async Task ClearIcon()
    {
        Editor.SetItemIcon(Node, Item, null);
        await OnToggleIcon.InvokeAsync(Item.Id);
        await OnChanged.InvokeAsync();
    }

    /// <summary>Icon picker contents, filtered by the search box (empty categories dropped).</summary>
    List<(string Category, List<string> Names)> FilteredIcons() =>
        Icons.Categories
            .Select(c => (c.Category, c.Names.Where(n => filter.Length == 0 || n.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase)).ToList()))
            .Where(c => c.Item2.Count > 0)
            .ToList();
}
