using BeDiagram.Services;
using BeDiagram.Services.Export;

namespace BeDiagram.Components.Pages;

/// <summary>Linked diagrams: following a shape or group link, the breadcrumb and Back, and the link picker.</summary>
public partial class Designer
{
    // ───────────── Linked diagrams ─────────────

    /// <summary>Diagrams we drilled down from via link badges, outermost first. Shown as a breadcrumb with Back.</summary>
    List<string> trail
    {
        get
        {
            return active.Trail;
        }
    }

    /// <summary>Where we're going once the unsaved-changes question is answered. KeepTrail = trail entries to keep (-1 = push current).</summary>
    (string Target, int KeepTrail)? pendingNav;

    void FollowLink(string? link)
    {
        if (!CanFollow(link))
        {
            return;
        }
        Navigate(link!, -1);
    }

    /// <summary>True when <paramref name="link"/> names another diagram that exists; otherwise says why not.</summary>
    bool CanFollow(string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return false;
        }
        if (hasFile && link == D.Name)
        {
            ShowToast("That link points to this diagram");
            return false;
        }
        if (!Store.Exists(link))
        {
            ShowToast($"Linked diagram “{link}” not found in {Store.Folder}");
            return false;
        }
        return true;
    }

    void GoBack()
    {
        if (trail.Count > 0)
        {
            GoUpTo(trail.Count - 1);
        }
    }

    /// <summary>Jump back to trail[level], dropping it and everything below it from the trail.</summary>
    void GoUpTo(int level)
    {
        if (level < 0 || level >= trail.Count)
        {
            return;
        }
        Navigate(trail[level], level);
    }

    void Navigate(string target, int keepTrail)
    {
        Editor.EndEdit();
        if (Editor.Dirty)
        {
            pendingNav = (target, keepTrail);
            dialog = Dialog.ConfirmLeave;
            return;
        }
        _ = DoNavigate(target, keepTrail);
    }

    async Task DoNavigate(string target, int keepTrail)
    {
        string? from = null;
        if (hasFile)
        {
            from = D.Name;
        }
        List<string> before = new List<string>(trail);
        if (!await Load(target))
        {
            return;
        }
        trail.Clear();
        if (keepTrail >= 0)
        {
            // Back to a level of the breadcrumb: keep the levels above it.
            for (int i = 0; i < keepTrail && i < before.Count; i++)
            {
                trail.Add(before[i]);
            }
        }
        else
        {
            // Following a link: the diagram we came from goes on the end of the breadcrumb.
            trail.AddRange(before);
            if (from is not null)
            {
                trail.Add(from);
            }
        }
        StateHasChanged();
    }

    async Task ConfirmLeave(bool save)
    {
        if (pendingNav is null)
        {
            CloseDialog();
            return;
        }
        (string Target, int KeepTrail) nav = pendingNav.Value;
        if (save)
        {
            await Save();
            if (Editor.Dirty)
            {
                return; // save failed: stay put, the toast says why
            }
        }
        pendingNav = null;
        CloseDialog();
        Editor.Dirty = false;
        await DoNavigate(nav.Target, nav.KeepTrail);
    }

    void CancelLeave()
    {
        pendingNav = null;
        CloseDialog();
    }

    /// <summary>Deleted from the Open dialog. A tab that had it open keeps the diagram, now unsaved.</summary>
    void DeleteFile(string name)
    {
        DiagramTab? open = TabFor(name);
        Store.Delete(name);
        if (open is not null)
        {
            open.HasFile = false;
            open.FileTime = null;
            open.Editor.Dirty = true;
        }
        ShowToast($"Deleted {name}");
    }

    async Task Save()
    {
        Editor.EndEdit();
        if (!hasFile)
        {
            ShowSaveAs();
            return;
        }
        if (FileChangedOnDisk())
        {
            dialog = Dialog.FileChanged;
            return;
        }
        await SaveNow();
    }

    /// <summary>
    /// The file was written by someone else since this tab opened or saved it, most likely the same diagram open in
    /// another window. Saving now would quietly throw their changes away, so Save asks first.
    /// </summary>
    bool FileChangedOnDisk()
    {
        if (active.FileTime is null)
        {
            return false;
        }
        DateTime? onDisk = Store.LastWritten(D.Name);
        if (onDisk is null)
        {
            return false;
        }
        return onDisk.Value != active.FileTime.Value;
    }

    async Task SaveNow()
    {
        try
        {
            string path = await Store.SaveAsync(D);
            Editor.Dirty = false;
            active.FileTime = Store.LastWritten(D.Name);
            ShowToast($"Saved {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            ShowToast($"Save failed: {ex.Message}");
        }
    }

    /// <summary>"Changed on disk" answered with Overwrite: save anyway.</summary>
    async Task OverwriteChanged()
    {
        CloseDialog();
        await SaveNow();
    }

    async Task ConfirmSaveAs(string name)
    {
        string safe = DiagramStore.SafeName(name);
        DiagramTab? other = TabFor(safe);
        if (other is not null && other != active)
        {
            ShowToast($"“{safe}” is open in another tab: close it first, or pick another name");
            return;
        }
        // Save as… from "save before closing?" closes the tab once it has saved.
        DiagramTab? closeAfter = closing;
        D.Name = safe;
        hasFile = true;
        // Save as has already asked about replacing an existing file, so take its current time as known.
        active.FileTime = Store.LastWritten(safe);
        CloseDialog();
        await Save();
        if (closeAfter is not null && closeAfter == active && !Editor.Dirty)
        {
            RemoveTab(active);
        }
    }

    /// <summary>
    /// Runs an Export menu entry: saved next to the diagram or copied to the clipboard, depending on its destination.
    /// The current-view area uses what is on screen now.
    /// </summary>
    async Task Export(ExportChoice choice, ExportOptions options)
    {
        try
        {
            ScreenView screen = new ScreenView(PanX, PanY, Zoom, canvasW, canvasH);
            string message = await Exports.ExportAsync(choice, D, screen, options);
            ShowToast(message);
        }
        catch (Exception ex)
        {
            ShowToast($"{choice.Format.Name} {choice.Destination.Verb} failed: {ex.Message}");
        }
    }

    void ShowToast(string message)
    {
        toast = message;
        if (toastCts is not null)
        {
            toastCts.Cancel();
        }
        CancellationTokenSource cts = new CancellationTokenSource();
        toastCts = cts;
        // Hide the toast after 3.5 s, unless a newer toast has replaced it (that cancels this wait).
        _ = Task.Delay(3500, cts.Token).ContinueWith(t =>
        {
            if (t.IsCanceled)
            {
                return;
            }
            toast = null;
            _ = InvokeAsync(StateHasChanged);
        }, TaskScheduler.Default);
    }
}
