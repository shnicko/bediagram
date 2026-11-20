using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using BeDiagram.Editing;

namespace BeDiagram.Components.Pages;

/// <summary>
/// Several diagrams open at once, one tab each (<see cref="DiagramTab"/>). Only the active tab is drawn and takes
/// input, so only it can be edited. Opening, closing and switching tabs, Ctrl+click on a link badge (new tab), moving
/// a tab to a new browser window (<c>/?open=name</c>) and telling the browser when a tab has unsaved changes.
/// </summary>
public partial class Designer
{
    // ───────────── Tabs ─────────────

    /// <summary>A diagram to open as soon as the page is ready: "Move to new window" opens <c>/?open=name</c>.</summary>
    [SupplyParameterFromQuery(Name = "open")]
    public string? OpenOnStart { get; set; }

    /// <summary>The start-up diagram was fitted before the browser sent the canvas size: fit again when it arrives.</summary>
    bool fitWhenSized;

    /// <summary>The tab waiting for Save / Discard before it closes (the close question, or Save as from it).</summary>
    DiagramTab? closing;

    /// <summary>The diagram waiting for "replace the open one, or a new tab?".</summary>
    string? pendingOpen;

    /// <summary>What designer.js was last told about unsaved changes in any tab.</summary>
    bool unsavedSent;

    /// <summary>The tab strip shows once there is more than one tab, so a single diagram looks as it always did.</summary>
    bool ShowTabs
    {
        get
        {
            return tabs.Count > 1;
        }
    }

    string AppClass
    {
        get
        {
            if (ShowTabs)
            {
                return "app with-tabs";
            }
            return "app";
        }
    }

    /// <summary>A new, empty tab just after the active one (not shown yet: <see cref="SwitchTo"/> shows it).</summary>
    DiagramTab AddTab()
    {
        DiagramTab tab = new DiagramTab(new DiagramEditor(this));
        tab.Editor.CopySettingsFrom(Editor);
        int at = tabs.IndexOf(active) + 1;
        tabs.Insert(at, tab);
        return tab;
    }

    /// <summary>
    /// Shows <paramref name="tab"/>. Whatever was going on in the old tab stops first: inline text is committed, a drag
    /// is cancelled and menus close. Auto-join, Snap and the angle lock carry over, so they feel the same in every tab.
    /// </summary>
    void SwitchTo(DiagramTab tab)
    {
        if (tab == active || !tabs.Contains(tab))
        {
            return;
        }
        Editor.EndEdit();
        CancelGesture();
        paletteDrop = null;
        CloseMenus();
        DiagramEditor from = Editor;
        active = tab;
        Editor.CopySettingsFrom(from);
    }

    void GoToTab(int index)
    {
        if (index >= 0 && index < tabs.Count)
        {
            SwitchTo(tabs[index]);
        }
    }

    /// <summary>The next (+1) or previous (−1) tab, going round from the last to the first.</summary>
    void StepTab(int delta)
    {
        int count = tabs.Count;
        int index = tabs.IndexOf(active);
        int next = ((index + delta) % count + count) % count;
        SwitchTo(tabs[next]);
    }

    /// <summary>The tab that has the file <paramref name="name"/> open, or null.</summary>
    DiagramTab? TabFor(string name)
    {
        foreach (DiagramTab t in tabs)
        {
            if (t.HasFile && t.Editor.Diagram.Name == name)
            {
                return t;
            }
        }
        return null;
    }

    bool AnyUnsaved()
    {
        foreach (DiagramTab t in tabs)
        {
            if (t.Editor.Dirty)
            {
                return true;
            }
        }
        return false;
    }

    // ───────────── New, open and close ─────────────

    /// <summary>File → New, Alt+N and the + button: a new tab (the empty start-up tab is simply reused).</summary>
    void NewDiagram()
    {
        if (active.IsFresh)
        {
            Reset();
            return;
        }
        DiagramTab tab = AddTab();
        SwitchTo(tab);
    }

    /// <summary>
    /// Open from the Open dialog. A diagram that is already open is switched to. The empty start-up tab is reused.
    /// Otherwise the user picks: replace the diagram on screen, or open in a new tab.
    /// </summary>
    async Task Open(string name)
    {
        DiagramTab? open = TabFor(name);
        if (open is not null)
        {
            CloseDialog();
            SwitchTo(open);
            ShowToast($"{name} is already open");
            return;
        }
        if (active.IsFresh)
        {
            if (await Load(name))
            {
                trail.Clear();
            }
            return;
        }
        pendingOpen = name;
        dialog = Dialog.OpenWhere;
    }

    /// <summary>The answer to "open where?": a new tab, or in place of the diagram on screen (asking about its unsaved
    /// changes first, as when following a link).</summary>
    async Task OpenWhere(bool newTab)
    {
        if (pendingOpen is null)
        {
            CloseDialog();
            return;
        }
        string name = pendingOpen;
        pendingOpen = null;
        if (newTab)
        {
            await OpenInNewTab(name, new List<string>());
            return;
        }
        CloseDialog();
        // Keep no breadcrumb: this is a fresh start in this tab, like opening it on its own.
        Navigate(name, 0);
    }

    /// <summary>Opens <paramref name="name"/> in a new tab with <paramref name="trailBefore"/> as its breadcrumb. If
    /// it can't be opened, the new tab goes again and the old one is shown.</summary>
    async Task<bool> OpenInNewTab(string name, List<string> trailBefore)
    {
        DiagramTab previous = active;
        DiagramTab tab = AddTab();
        SwitchTo(tab);
        if (!await Load(name))
        {
            RemoveTab(tab);
            SwitchTo(previous);
            return false;
        }
        trail.AddRange(trailBefore);
        return true;
    }

    /// <summary>Ctrl+click on a link badge: the linked diagram in a new tab, whose Back returns to this diagram.</summary>
    async Task FollowLinkInNewTab(string? link)
    {
        if (!CanFollow(link))
        {
            return;
        }
        string target = link!;
        DiagramTab? open = TabFor(target);
        if (open is not null)
        {
            SwitchTo(open);
            ShowToast($"{target} is already open");
            return;
        }
        List<string> before = new List<string>(trail);
        if (hasFile)
        {
            before.Add(D.Name);
        }
        await OpenInNewTab(target, before);
    }

    /// <summary>The × on a tab, Alt+W and the tab menu: closes it, asking first when it has unsaved changes (the tab
    /// is shown so the question is about what's on screen).</summary>
    void CloseTab(DiagramTab tab)
    {
        if (tab == active)
        {
            Editor.EndEdit();
        }
        if (tab.Editor.Dirty)
        {
            SwitchTo(tab);
            CloseMenus();
            closing = tab;
            dialog = Dialog.ConfirmClose;
            return;
        }
        RemoveTab(tab);
    }

    /// <summary>Takes the tab away without asking. The tab to its right (or else its left) takes its place; closing
    /// the last tab leaves a new empty one.</summary>
    void RemoveTab(DiagramTab tab)
    {
        int index = tabs.IndexOf(tab);
        if (index < 0)
        {
            return;
        }
        bool wasActive = tab == active;
        if (wasActive)
        {
            CancelGesture();
            paletteDrop = null;
        }
        DiagramEditor settings = Editor;
        tabs.RemoveAt(index);
        if (tabs.Count == 0)
        {
            DiagramTab fresh = new DiagramTab(new DiagramEditor(this));
            fresh.Editor.CopySettingsFrom(settings);
            tabs.Add(fresh);
        }
        if (wasActive)
        {
            active = tabs[Math.Min(index, tabs.Count - 1)];
            Editor.CopySettingsFrom(settings);
        }
    }

    /// <summary>The answer to "save before closing?": true = save (or Save as for a new diagram), false = discard.</summary>
    async Task ConfirmClose(bool save)
    {
        if (closing is null)
        {
            CloseDialog();
            return;
        }
        DiagramTab tab = closing;
        if (save)
        {
            if (!hasFile)
            {
                // Save as… closes the tab once it has saved (see ConfirmSaveAs); cancelling it keeps the tab.
                ShowSaveAs();
                return;
            }
            await Save();
            if (Editor.Dirty)
            {
                return; // save failed or asked about the file on disk: keep the tab, the toast or dialog says why
            }
        }
        CloseDialog();
        RemoveTab(tab);
    }

    // ───────────── Another window ─────────────

    /// <summary>
    /// The address that opens <paramref name="tab"/>'s diagram in a new browser window, or null when it can't move
    /// (never saved, or unsaved changes: the new window opens the file, so save first).
    /// </summary>
    string? WindowLink(DiagramTab tab)
    {
        if (!tab.HasFile || tab.Editor.Dirty)
        {
            return null;
        }
        return "/?open=" + Uri.EscapeDataString(tab.Editor.Diagram.Name);
    }

    /// <summary>The tab's diagram has been opened in a new window (the menu item is a link): take it away here.</summary>
    void MovedToWindow(DiagramTab tab)
    {
        CloseMenus();
        RemoveTab(tab);
    }

    /// <summary>A window opened from "Move to new window": open its diagram in the first tab.</summary>
    async Task OpenStartDiagram()
    {
        if (string.IsNullOrWhiteSpace(OpenOnStart))
        {
            return;
        }
        string name = OpenOnStart;
        OpenOnStart = null;
        if (await Load(name))
        {
            fitWhenSized = true;
        }
        StateHasChanged();
    }

    /// <summary>After a render: tell designer.js whether closing the browser tab should ask first.</summary>
    async Task SendUnsaved()
    {
        bool any = AnyUnsaved();
        if (module is null || any == unsavedSent)
        {
            return;
        }
        unsavedSent = any;
        await module.InvokeVoidAsync("setUnsaved", any);
    }
}
