using Microsoft.AspNetCore.Components;
using BeDiagram.Services;

namespace BeDiagram.Components.Dialogs;

/// <summary>
/// The Open dialog: the saved diagrams in the storage folder, newest first as <see cref="DiagramStore.List"/> gives
/// them, each with a delete button that asks once more. Opening and deleting are the page's (it knows which diagram is
/// open); the list is read when the dialog opens and again after a delete.
/// </summary>
public partial class OpenDialog
{
    [Inject] DiagramStore Store { get; set; } = default!;

    /// <summary>The open diagram has unsaved changes (shows a warning).</summary>
    [Parameter] public bool Dirty { get; set; }
    [Parameter] public EventCallback<string> OnOpen { get; set; }
    [Parameter] public EventCallback<string> OnDelete { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    IReadOnlyList<(string Name, DateTime Modified)> files = Array.Empty<(string Name, DateTime Modified)>();

    /// <summary>The file whose delete button has been pressed once (it then asks to confirm).</summary>
    string? confirmDelete;

    protected override void OnInitialized() => files = Store.List();

    void AskDelete(string name) => confirmDelete = name;

    async Task Delete(string name)
    {
        await OnDelete.InvokeAsync(name);
        files = Store.List();
        confirmDelete = null;
    }
}
