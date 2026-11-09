using Microsoft.AspNetCore.Components;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Kinds;

namespace BeDiagram.Components.Shared;

/// <summary>
/// The rows of a list or input–process–output node in the side panel: add, edit, reorder, remove, pick icons, and
/// for an IPO box the section titles and side-by-side / stacked layout. All changes go through
/// <see cref="DiagramEditor"/> (one undo step each; typing records undo when the box gets focus).
/// </summary>
public partial class PanelItems
{
    [Parameter, EditorRequired] public DiagramEditor Editor { get; set; } = default!;

    [Parameter, EditorRequired] public Node Node { get; set; } = default!;

    /// <summary>Called after every change, so the page redraws the canvas.</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    /// <summary>The row whose icon picker is open, or null.</summary>
    string? iconFor;

    bool IsIpo => Node.Kind is IpoKind;

    string SectionValue(IpoSection section)
    {
        string t = Node.SectionTitle(section);
        return t == IpoSections.DefaultTitle(section) ? "" : t;
    }

    Task SectionTyped(IpoSection section, ChangeEventArgs e)
    {
        Editor.SetSectionTitle(Node, section, e.Value?.ToString());
        return OnChanged.InvokeAsync();
    }

    Task StackedChanged(ChangeEventArgs e)
    {
        Editor.SetStacked(Node, e.Value is true);
        return OnChanged.InvokeAsync();
    }

    Task Add(IpoSection? section)
    {
        Editor.AddItem(Node, section);
        iconFor = null;
        return OnChanged.InvokeAsync();
    }

    void ToggleIcon(string itemId) => iconFor = iconFor == itemId ? null : itemId;

    Task Changed() => OnChanged.InvokeAsync();
}
