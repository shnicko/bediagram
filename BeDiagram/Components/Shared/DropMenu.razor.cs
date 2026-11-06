using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Shared;

/// <summary>
/// One heading of the menu bar (File, Edit, …) with its drop-down. The page keeps which menu is open
/// (<see cref="OpenMenu"/>), so only one is open at a time. Clicking an item closes the menu; wrap settings that
/// should stay open (dropdowns, checkboxes) in an element with <c>@onclick:stopPropagation</c>.
/// </summary>
public partial class DropMenu
{
    /// <summary>The heading text, e.g. "File".</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>The name of the menu that's open on the page, or null when none is.</summary>
    [Parameter] public string? OpenMenu { get; set; }

    [Parameter] public EventCallback<string?> OpenMenuChanged { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    bool Open => OpenMenu == Title;

    string HeadClass => Open ? "menu-head on" : "menu-head";

    Task Toggle() => OpenMenuChanged.InvokeAsync(Open ? null : Title);

    Task Close() => OpenMenuChanged.InvokeAsync(null);
}
