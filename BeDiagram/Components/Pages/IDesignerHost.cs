using BeDiagram.Components.Pages.Commands;
using BeDiagram.Editing;
using BeDiagram.Services.Export;

namespace BeDiagram.Components.Pages;

/// <summary>
/// What the page's child components (side panels, toolbar, palette, dialogs) may ask of the page — the Mediator: the
/// parts don't know about each other, only about this. The Designer implements it in Designer.Host.cs. Anything that
/// changes the diagram goes to <see cref="Editor"/>; the rest here is what only the page can do (dialogs, navigation,
/// view). If a part needs something new from the page, add it here and there.
/// </summary>
public interface IDesignerHost
{
    /// <summary>The open diagram, the selection and every editing action.</summary>
    DiagramEditor Editor { get; }

    /// <summary>Run, grey out and describe commands (the same ones the keyboard runs).</summary>
    CommandButtons Buttons { get; }

    /// <summary>Redraws the whole page. <see cref="DesignerPart"/> calls it after every event in a part, so a click in a
    /// panel redraws the canvas too.</summary>
    void Redraw();

    // ───────────── Dialogs and files ─────────────

    /// <summary>Opens the step order pop-up.</summary>
    void ShowStepOrder();

    /// <summary>Opens the keyboard shortcuts pop-up.</summary>
    void ShowShortcuts();

    /// <summary>Opens the JSON editor on the whole diagram (the canvas waits until it closes).</summary>
    void ShowJson();

    /// <summary>Starts a new diagram in a new tab.</summary>
    void NewDiagram();

    /// <summary>Shows the Open dialog.</summary>
    void ShowOpen();

    /// <summary>Runs an Export menu entry (saved next to the diagram or copied to the clipboard); the current-view area uses what is on screen now.</summary>
    Task Export(ExportChoice choice, ExportOptions options);

    /// <summary>The format painter is on (the brush button shows lit).</summary>
    bool Painting { get; }

    // ───────────── Tabs ─────────────

    /// <summary>The open diagrams, in tab order.</summary>
    IReadOnlyList<DiagramTab> Tabs { get; }

    /// <summary>The tab on screen (the only one that can be edited).</summary>
    DiagramTab ActiveTab { get; }

    /// <summary>Shows a tab.</summary>
    void SwitchTab(DiagramTab tab);

    /// <summary>Closes a tab, asking first when it has unsaved changes.</summary>
    void CloseTab(DiagramTab tab);

    /// <summary>The address that opens the tab's diagram in a new window, or null when it must be saved first.</summary>
    string? WindowLink(DiagramTab tab);

    /// <summary>The tab's diagram was opened in a new window: close the tab here.</summary>
    void MovedToWindow(DiagramTab tab);

    // ───────────── Linked diagrams ─────────────

    /// <summary>Opens the linked diagram (after asking about unsaved changes).</summary>
    void FollowLink(string? link);

    /// <summary>Ctrl+click on a link badge: opens the linked diagram in a new tab.</summary>
    Task FollowLinkInNewTab(string? link);

    /// <summary>The open diagram's own name, left out of the link pickers (null when it has never been saved).</summary>
    string? LinkExclude { get; }

    /// <summary>Diagrams we drilled down from via links, outermost first (the breadcrumb).</summary>
    IReadOnlyList<string> Trail { get; }

    /// <summary>Back up one level of the breadcrumb.</summary>
    void GoBack();

    /// <summary>Back to <c>Trail[level]</c>.</summary>
    void GoUpTo(int level);

    // ───────────── Page and view ─────────────

    /// <summary>Paper size by key (<see cref="Models.PageSetup"/>), or open canvas; fits the view to it.</summary>
    void SetPageSize(string? size);

    /// <summary>Portrait or landscape; fits the view to the page.</summary>
    void SetLandscape(bool landscape);

    /// <summary>Pointer presses on items in the canvas layers.</summary>
    ICanvasInput Canvas { get; }

    /// <summary>Zoom of the canvas (1 = 100%).</summary>
    double Zoom { get; }

    /// <summary>The background grid (<see cref="Models.Styles.GridLook"/> key): a view setting, not saved in the file.</summary>
    string Grid { get; }

    void SetGrid(string grid);

    /// <summary>The menu bar heading whose drop-down is open ("File", "Edit", …), or null.</summary>
    string? OpenMenu { get; }

    void SetOpenMenu(string? menu);

    /// <summary>The message shown in the status bar for a few seconds, or null.</summary>
    string? Toast { get; }

    /// <summary>What a drop from the palette adds at the drop point (world x, y); set when a tile drag starts.</summary>
    void SetPaletteDrop(Action<double, double> drop);
}
