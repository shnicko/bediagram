using BeDiagram.Components.Pages.Commands;
using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Shared;

/// <summary>Help → Keyboard shortcuts: every command's keys (from the registry) and the mouse gestures.</summary>
public partial class ShortcutsDialog
{
    [Parameter, EditorRequired] public CommandRegistry Commands { get; set; } = default!;

    [Parameter] public EventCallback OnClose { get; set; }

    List<ShortcutRow> KeyRows => ShortcutList.Keys(Commands);

    Task Close() => OnClose.InvokeAsync();
}
