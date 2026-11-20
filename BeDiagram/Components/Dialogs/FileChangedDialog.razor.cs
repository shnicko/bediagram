using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Dialogs;

/// <summary>
/// Asked by Save when the file on disk is newer than when this tab opened or saved it (the same diagram saved from
/// another window): overwrite it, save under another name, or cancel.
/// </summary>
public partial class FileChangedDialog
{
    [Parameter] public string DiagramName { get; set; } = "";
    [Parameter] public EventCallback OnOverwrite { get; set; }
    [Parameter] public EventCallback OnSaveAs { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }
}
