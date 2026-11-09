using BeDiagram.Services;
using Microsoft.JSInterop;
using BeDiagram.Models;
using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Pages;

/// <summary>Open, save, save as, new and delete diagrams (DiagramStore), with the unsaved-changes prompts.</summary>
public partial class Designer
{
    // ───────────── Files ─────────────

    void Reset(bool keepTrail = false)
    {
        if (!keepTrail) trail.Clear();
        Editor.Load(new Diagram());
        hasFile = false;
        Zoom = 1; PanX = 40; PanY = 40;
    }

    void NewDiagram()
    {
        if (Editor.Dirty) dialog = Dialog.ConfirmNew;
        else Reset();
    }

    void ShowOpen()
    {
        confirmDelete = null;
        openFiles = Store.List();
        dialog = Dialog.Open;
    }

    void ShowSaveAs()
    {
        saveAsName = D.Name;
        dialog = Dialog.SaveAs;
        _ = module?.InvokeVoidAsync("focus", "saveas-name");
    }

    /// <summary>True for a group not inside another group (one that can be a swim lane).</summary>
    bool IsTopLevel(Group g) => Editor.Tree.Parent(g) is null;

    /// <summary>Selected groups that are lanes (top-level); with two or more, Arrange works on just those.</summary>
    int SelectedLaneCount => Editor.TopLevelGroups().Count(g => Editor.Selection.Contains(g.Id));

    /// <summary>Opens the step order pop-up (Components/Shared/StepOrderDialog).</summary>
    void ShowStepOrder() => dialog = Dialog.StepOrder;

    void ShowShortcuts() => dialog = Dialog.Shortcuts;

    void CloseDialog() { dialog = Dialog.None; confirmDelete = null; openMenu = null; }

    /// <summary>Open from the Open dialog: a fresh start, so the linked-diagram trail is cleared.</summary>
    async Task Open(string name)
    {
        if (await Load(name)) trail.Clear();
    }

    async Task<bool> Load(string name)
    {
        try
        {
            Diagram loaded = await Store.LoadAsync(name);
            Reset(keepTrail: true);
            Editor.Load(loaded);
            hasFile = true;
            CloseDialog();
            Fit();
            ShowToast($"Opened {name}");
            return true;
        }
        catch (Exception ex)
        {
            ShowToast($"Could not open {name}: {ex.Message}");
            return false;
        }
    }

    bool SaveAsWillOverwrite => Store.Exists(saveAsName) && DiagramStore.SafeName(saveAsName) != (hasFile ? D.Name : null);

    string SaveAsPath => Path.Combine(Store.Folder, DiagramStore.SafeName(saveAsName) + DiagramStore.Extension);

    void RenameDiagram(ChangeEventArgs e)
    {
        D.Name = DiagramStore.SafeName(e.Value?.ToString() ?? string.Empty);
        Editor.MarkDirty();
    }
}
