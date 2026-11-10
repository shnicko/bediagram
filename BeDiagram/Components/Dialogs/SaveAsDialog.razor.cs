using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Services;

namespace BeDiagram.Components.Dialogs;

/// <summary>
/// The Save as dialog: a name (starting with the diagram's), the file it will be saved to, and a warning when that
/// would overwrite another diagram. Enter or Save hands the name to the page, which saves.
/// </summary>
public partial class SaveAsDialog
{
    [Inject] DiagramStore Store { get; set; } = default!;

    /// <summary>The name the box starts with (the diagram's name).</summary>
    [Parameter] public string Name { get; set; } = "";
    /// <summary>The name the open diagram is saved under, or null when it has never been saved: saving over that one
    /// isn't an overwrite.</summary>
    [Parameter] public string? SavedName { get; set; }
    [Parameter] public EventCallback<string> OnSave { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    string name = "";

    protected override void OnInitialized() => name = Name;

    bool WillOverwrite => Store.Exists(name) && DiagramStore.SafeName(name) != SavedName;

    string SavePath => Path.Combine(Store.Folder, DiagramStore.SafeName(name) + DiagramStore.Extension);

    Task Save() => OnSave.InvokeAsync(name);

    async Task Key(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await Save();
    }
}
