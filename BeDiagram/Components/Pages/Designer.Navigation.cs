using BeDiagram.Services;
using BeDiagram.Services.Export;

namespace BeDiagram.Components.Pages;

/// <summary>Linked diagrams: following a shape or group link, the breadcrumb and Back, and the link picker.</summary>
public partial class Designer
{
    // ───────────── Linked diagrams ─────────────

    /// <summary>Diagrams we drilled down from via link badges, outermost first. Shown as a breadcrumb with Back.</summary>
    readonly List<string> trail = new();

    /// <summary>Where we're going once the unsaved-changes question is answered. KeepTrail = trail entries to keep (-1 = push current).</summary>
    (string Target, int KeepTrail)? pendingNav;

    void FollowLink(string? link)
    {
        if (string.IsNullOrWhiteSpace(link)) return;
        if (hasFile && link == D.Name) { ShowToast("That link points to this diagram"); return; }
        if (!Store.Exists(link)) { ShowToast($"Linked diagram “{link}” not found in {Store.Folder}"); return; }
        Navigate(link, -1);
    }

    void GoBack() { if (trail.Count > 0) GoUpTo(trail.Count - 1); }

    /// <summary>Jump back to trail[level], dropping it and everything below it from the trail.</summary>
    void GoUpTo(int level)
    {
        if (level < 0 || level >= trail.Count) return;
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
        string? from = hasFile ? D.Name : null;
        List<string> before = trail.ToList();
        if (!await Load(target)) return;
        trail.Clear();
        if (keepTrail >= 0) trail.AddRange(before.Take(keepTrail));
        else
        {
            trail.AddRange(before);
            if (from is not null) trail.Add(from);
        }
        StateHasChanged();
    }

    async Task ConfirmLeave(bool save)
    {
        if (pendingNav is not { } nav) { CloseDialog(); return; }
        if (save)
        {
            await Save();
            if (Editor.Dirty) return; // save failed: stay put, the toast says why
        }
        pendingNav = null;
        CloseDialog();
        Editor.Dirty = false;
        await DoNavigate(nav.Target, nav.KeepTrail);
    }

    void CancelLeave() { pendingNav = null; CloseDialog(); }

    void DeleteFile(string name)
    {
        Store.Delete(name);
        if (hasFile && D.Name == name) { hasFile = false; Editor.Dirty = true; }
        ShowToast($"Deleted {name}");
    }

    async Task Save()
    {
        Editor.EndEdit();
        if (!hasFile) { ShowSaveAs(); return; }
        try
        {
            string path = await Store.SaveAsync(D);
            Editor.Dirty = false;
            ShowToast($"Saved {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            ShowToast($"Save failed: {ex.Message}");
        }
    }

    async Task ConfirmSaveAs(string name)
    {
        D.Name = DiagramStore.SafeName(name);
        hasFile = true;
        CloseDialog();
        await Save();
    }

    /// <summary>Exports next to the saved diagram; the current-view area uses what is on screen now.</summary>
    async Task Export(ExportChoice choice, ExportOptions options)
    {
        try
        {
            ScreenView screen = new ScreenView(PanX, PanY, Zoom, canvasW, canvasH);
            (string path, string? note) = await Exports.ExportAsync(choice, D, screen, options);
            ShowToast(note is null ? $"Exported {Path.GetFileName(path)}" : $"Exported {Path.GetFileName(path)} ({note})");
        }
        catch (Exception ex)
        {
            ShowToast($"{choice.Format.Name} export failed: {ex.Message}");
        }
    }

    void ShowToast(string message)
    {
        toast = message;
        toastCts?.Cancel();
        CancellationTokenSource cts = toastCts = new CancellationTokenSource();
        _ = Task.Delay(3500, cts.Token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            toast = null;
            _ = InvokeAsync(StateHasChanged);
        }, TaskScheduler.Default);
    }
}
