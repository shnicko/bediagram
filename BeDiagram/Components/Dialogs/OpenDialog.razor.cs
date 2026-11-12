using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Services;

namespace BeDiagram.Components.Dialogs;

/// <summary>
/// The Open dialog: browses the storage folder and its subfolders. It starts in the open diagram's folder and shows a
/// path to click back up, the subfolders (by name), then that folder's diagrams newest first, each with a delete button
/// that asks once more. <i>New folder</i> makes a subfolder here and goes into it; an empty folder can be deleted. Typing
/// in the find box lists matching diagrams from every folder instead. Opening and deleting diagrams are the page's (it
/// knows which diagram is open); folders are this dialog's own business.
/// </summary>
public partial class OpenDialog
{
    [Inject] DiagramStore Store { get; set; } = default!;

    /// <summary>The open diagram has unsaved changes (shows a warning).</summary>
    [Parameter] public bool Dirty { get; set; }
    /// <summary>The folder to start in ("" = the top), usually the open diagram's. The top if it no longer exists.</summary>
    [Parameter] public string StartFolder { get; set; } = "";
    [Parameter] public EventCallback<string> OnOpen { get; set; }
    [Parameter] public EventCallback<string> OnDelete { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>The folder being shown ("" = the top).</summary>
    string folder = "";
    IReadOnlyList<string> folders = Array.Empty<string>();
    IReadOnlyList<(string Name, DateTime Modified)> files = Array.Empty<(string Name, DateTime Modified)>();
    /// <summary>Every diagram in every folder, for the find box.</summary>
    IReadOnlyList<(string Name, DateTime Modified)> all = Array.Empty<(string Name, DateTime Modified)>();

    string find = "";

    /// <summary>The file whose delete button has been pressed once (it then asks to confirm).</summary>
    string? confirmDelete;

    /// <summary>The name typed for a new folder; null when not making one.</summary>
    string? newFolder;
    string? folderError;
    ElementReference newFolderInput;
    bool focusNewFolder;

    protected override void OnInitialized()
    {
        string start = DiagramStore.SafeFolder(StartFolder);
        Show(Store.FolderExists(start) ? start : "");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!focusNewFolder) return;
        focusNewFolder = false;
        await newFolderInput.FocusAsync();
    }

    void Show(string path)
    {
        folder = path;
        folders = Store.Folders(path);
        files = Store.Files(path);
        all = Store.List();
        confirmDelete = null;
        newFolder = null;
        folderError = null;
    }

    bool AtTop => folder.Length == 0;

    bool Finding => find.Trim().Length > 0;

    void Up() => Show(DiagramStore.FolderOf(folder));

    /// <summary>The diagrams listed: this folder's, or every folder's matches while finding.</summary>
    IEnumerable<(string Name, DateTime Modified)> Shown =>
        Finding ? all.Where(f => f.Name.Contains(find.Trim(), StringComparison.OrdinalIgnoreCase)) : files;

    /// <summary>The storage folder's own name, standing for the top of the tree.</summary>
    string TopLabel => Path.GetFileName(Store.Folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

    /// <summary>The path above the list: the top, then each folder down to this one.</summary>
    IReadOnlyList<(string Label, string Path)> Crumbs
    {
        get
        {
            List<(string Label, string Path)> crumbs = new() { (TopLabel, "") };
            string path = "";
            foreach (string part in folder.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                path = DiagramStore.Join(path, part);
                crumbs.Add((part, path));
            }
            return crumbs;
        }
    }

    static string Leaf(string name) => DiagramStore.LeafOf(name);

    /// <summary>Where a found diagram lives, for the find list.</summary>
    string FolderLabel(string name)
    {
        string f = DiagramStore.FolderOf(name);
        return f.Length == 0 ? TopLabel : f;
    }

    string? EmptyNote =>
        Finding ? (Shown.Any() ? null : $"No diagram names contain “{find.Trim()}”.")
        : folders.Count > 0 || files.Count > 0 ? null
        : AtTop ? $"No saved diagrams yet in {Store.Folder}"
        : "This folder is empty.";

    bool CanDelete(string path) => Store.IsEmpty(path);

    void DeleteFolder(string path)
    {
        Store.DeleteFolder(path);
        Show(folder);
    }

    void StartNewFolder()
    {
        newFolder = "";
        folderError = null;
        focusNewFolder = true;
    }

    void CancelNewFolder()
    {
        newFolder = null;
        folderError = null;
    }

    void CreateFolder()
    {
        try
        {
            Show(Store.CreateFolder(folder, newFolder ?? ""));
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            folderError = ex.Message;
        }
    }

    void NewFolderKey(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") CreateFolder();
        else if (e.Key == "Escape") CancelNewFolder();
    }

    void AskDelete(string name) => confirmDelete = name;

    async Task Delete(string name)
    {
        await OnDelete.InvokeAsync(name);
        Show(folder);
    }
}
