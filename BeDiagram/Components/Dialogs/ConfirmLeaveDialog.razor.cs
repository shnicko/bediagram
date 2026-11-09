using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Dialogs;

/// <summary>
/// Asked before following a link (or going back) with unsaved changes: cancel, discard and open, or save and open
/// (only offered when the diagram has a file to save to).
/// </summary>
public partial class ConfirmLeaveDialog
{
    [Parameter] public string DiagramName { get; set; } = "";
    /// <summary>The diagram about to be opened.</summary>
    [Parameter] public string Target { get; set; } = "";
    /// <summary>The open diagram has been saved before, so it can be saved now and reached again with Back.</summary>
    [Parameter] public bool HasFile { get; set; }
    /// <summary>Go ahead: true to save first, false to discard the changes.</summary>
    [Parameter] public EventCallback<bool> OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
}
