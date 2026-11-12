using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Services;

namespace BeDiagram.Components.Dialogs;

/// <summary>
/// The Save as dialog: a folder (any folder under the storage folder, starting with the diagram's) and a name (starting
/// with the diagram's), the file it will be saved to, and a warning when that would overwrite another diagram. A name
/// with '/' in it adds folders below the chosen one, made on save. Enter or Save hands the full name to the page, which saves.
/// </summary>
public partial class SaveAsDialog
{
    [Inject] DiagramStore Store { get; set; } = default!;

    /// <summary>The name the dialog starts with (the diagram's, folder included).</summary>
    [Parameter] public string Name { get; set; } = "";
    /// <summary>The name the open diagram is saved under, or null when it has never been saved: saving over that one
    /// isn't an overwrite.</summary>
    [Parameter] public string? SavedName { get; set; }
    [Parameter] public EventCallback<string> OnSave { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    string folder = "";
    string name = "";
    IReadOnlyList<string> folders = Array.Empty<string>();

    protected override void OnInitialized()
    {
        string start = DiagramStore.SafeName(Name);
        folder = DiagramStore.FolderOf(start);
        name = DiagramStore.LeafOf(start);
        List<string> all = Store.AllFolders().ToList();
        if (folder.Length > 0 && !all.Contains(folder)) all.Add(folder); // its folder has gone: saving makes it again
        folders = all.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
    }

    void FolderPicked(ChangeEventArgs e) => folder = e.Value?.ToString() ?? "";

    /// <summary>The storage folder's own name, standing for the top of the tree.</summary>
    string TopLabel => Path.GetFileName(Store.Folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    /// <summary>The diagram name that will be saved: the folder and the typed name, cleaned.</summary>
    string Full => DiagramStore.SafeName(DiagramStore.Join(folder, name));

    bool WillOverwrite => Store.Exists(Full) && Full != SavedName;

    string SavePath => Path.Combine(Store.Folder, Full.Replace('/', Path.DirectorySeparatorChar) + DiagramStore.Extension);

    Task Save() => OnSave.InvokeAsync(Full);

    async Task Key(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await Save();
    }
}
