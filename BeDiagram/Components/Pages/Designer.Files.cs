using BeDiagram.Models;
using BeDiagram.Services;

namespace BeDiagram.Components.Pages;

/// <summary>Open (into the active tab), the dialogs, and the names the dialogs read. New and Open into a new tab are in
/// Designer.Tabs.cs; Save and Save as in Designer.Navigation.cs.</summary>
public partial class Designer
{
    // ───────────── Files ─────────────

    /// <summary>An empty diagram in the active tab, view back to the start.</summary>
    void Reset(bool keepTrail = false)
    {
        if (!keepTrail)
        {
            trail.Clear();
        }
        Editor.Load(new Diagram());
        hasFile = false;
        active.FileTime = null;
        Zoom = 1;
        PanX = 40;
        PanY = 40;
    }

    void ShowOpen()
    {
        dialog = Dialog.Open;
    }

    void ShowSaveAs()
    {
        dialog = Dialog.SaveAs;
        FocusAfterRender("saveas-name");
    }

    /// <summary>Opens the step order pop-up (Components/Dialogs/StepOrderDialog).</summary>
    void ShowStepOrder()
    {
        dialog = Dialog.StepOrder;
    }

    void ShowShortcuts()
    {
        dialog = Dialog.Shortcuts;
    }

    /// <summary>Opens the JSON editor (Components/Dialogs/JsonDialog) on the whole diagram.</summary>
    void ShowJson()
    {
        CloseMenus();
        dialog = Dialog.Json;
        FocusAfterRender("json-text");
    }

    /// <summary>The JSON editor replaced the diagram: close it and say how to get the old one back.</summary>
    void JsonApplied()
    {
        CloseDialog();
        ShowToast("Diagram updated from JSON (Ctrl+Z to undo)");
    }

    /// <summary>Closes any dialog and menu; a close or open that was waiting on the dialog is forgotten.</summary>
    void CloseDialog()
    {
        dialog = Dialog.None;
        openMenu = null;
        closing = null;
        pendingOpen = null;
    }

    /// <summary>Opens <paramref name="name"/> into the active tab, replacing what it shows (callers have already
    /// asked about unsaved changes). Returns false, with a toast, when it can't be read.</summary>
    async Task<bool> Load(string name)
    {
        try
        {
            Diagram loaded = await Store.LoadAsync(name);
            Reset(keepTrail: true);
            Editor.Load(loaded);
            hasFile = true;
            active.FileTime = Store.LastWritten(name);
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
    string? SavedName
    {
        get
        {
            if (hasFile)
            {
                return D.Name;
            }
            return null;
        }
    }

    /// <summary>The folder the Open dialog starts in: the open diagram's, or the top when it has never been saved.</summary>
    string OpenFolder
    {
        get
        {
            if (hasFile)
            {
                return DiagramStore.FolderOf(D.Name);
            }
            return "";
        }
    }
}
