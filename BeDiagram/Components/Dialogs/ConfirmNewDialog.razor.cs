using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Dialogs;

/// <summary>Asked before File › New when the open diagram has unsaved changes.</summary>
public partial class ConfirmNewDialog
{
    [Parameter] public string DiagramName { get; set; } = "";
    [Parameter] public EventCallback OnDiscard { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
}
