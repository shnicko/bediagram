using Microsoft.JSInterop;
using BeDiagram.Models;
using BeDiagram.Services;

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

    void ShowOpen() => dialog = Dialog.Open;

    void ShowSaveAs()
    {
        dialog = Dialog.SaveAs;
        FocusAfterRender("saveas-name");
    }

    /// <summary>Opens the step order pop-up (Components/Dialogs/StepOrderDialog).</summary>
    void ShowStepOrder() => dialog = Dialog.StepOrder;

    void ShowShortcuts() => dialog = Dialog.Shortcuts;

    void CloseDialog() { dialog = Dialog.None; openMenu = null; }

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

    /// <summary>The name the open diagram is saved under, or null when it has never been saved.</summary>
    string? SavedName => hasFile ? D.Name : null;

    /// <summary>The folder the Open dialog starts in: the open diagram's, or the top when it has never been saved.</summary>
    string OpenFolder => hasFile ? DiagramStore.FolderOf(D.Name) : "";
}
