using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Dialogs;

/// <summary>
/// Asked before closing a tab with unsaved changes: cancel, discard and close, or save and close (Save as… for a
/// diagram that has never been saved; the tab closes once it is saved).
/// </summary>
public partial class ConfirmCloseDialog
{
    [Parameter] public string DiagramName { get; set; } = "";
    /// <summary>The diagram has a file, so it can be saved straight away.</summary>
    [Parameter] public bool HasFile { get; set; }
    /// <summary>Go ahead: true to save first, false to discard the changes.</summary>
    [Parameter] public EventCallback<bool> OnConfirm { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    Task Discard()
    {
        return OnConfirm.InvokeAsync(false);
    }

    Task SaveFirst()
    {
        return OnConfirm.InvokeAsync(true);
    }
}
