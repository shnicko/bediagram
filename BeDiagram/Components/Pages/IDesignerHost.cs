using BeDiagram.Components.Pages.Commands;
using BeDiagram.Editing;

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

    /// <summary>Opens the step order pop-up.</summary>
    void ShowStepOrder();

    /// <summary>Opens the linked diagram (after asking about unsaved changes).</summary>
    void FollowLink(string? link);

    /// <summary>The open diagram's own name, left out of the link pickers (null when it has never been saved).</summary>
    string? LinkExclude { get; }

    /// <summary>Paper size by key (<see cref="Models.PageSetup"/>), or open canvas; fits the view to it.</summary>
    void SetPageSize(string? size);

    /// <summary>Portrait or landscape; fits the view to the page.</summary>
    void SetLandscape(bool landscape);
}
