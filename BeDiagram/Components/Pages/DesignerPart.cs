using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages.Commands;
using BeDiagram.Editing;
using BeDiagram.Models;

namespace BeDiagram.Components.Pages;

/// <summary>
/// Base class for a part of the Designer page (a side panel, the toolbar, …). It talks to the page only through
/// <see cref="Host"/>.
/// <para>
/// Blazor re-renders only the component that handled an event, so a click in a panel would leave the canvas stale.
/// A part therefore redraws the whole page after every event it handles (<see cref="IHandleEvent"/>), exactly as when
/// its markup was inside Designer.razor — handlers don't need to remember to.
/// </para>
/// </summary>
public abstract class DesignerPart : ComponentBase, IHandleEvent
{
    [Parameter, EditorRequired] public IDesignerHost Host { get; set; } = default!;

    protected DiagramEditor Editor => Host.Editor;

    /// <summary>Short name for the open diagram, as in the Designer.</summary>
    protected Diagram D => Host.Editor.Diagram;

    /// <summary>The commands with buttons: <c>Run(Cmd.Delete)</c>, <c>CanRun(Cmd.Branch)</c>, <c>Tip(Cmd.Branch)</c>.</summary>
    protected CommandButtons Cmd => Host.Buttons;

    protected Task Run(IEditorCommand command) => Host.Buttons.Run(command);

    protected bool CanRun(IEditorCommand command) => Host.Buttons.CanRun(command);

    protected static string Tip(IEditorCommand command) => CommandButtons.Tip(command);

    protected static string KeyText(IEditorCommand command) => CommandButtons.KeyText(command);

    /// <summary>For the <c>OnChanged</c> of a child component (ShapeStyle, PanelItems, …): redraw the page.</summary>
    protected void Redraw() => Host.Redraw();

    /// <summary>Same as <see cref="ComponentBase"/>'s handler (draw now, and again when an async handler finishes),
    /// but drawing the whole page.</summary>
    async Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    {
        Task task = callback.InvokeAsync(arg);
        bool pending = task.Status != TaskStatus.RanToCompletion && task.Status != TaskStatus.Canceled;
        Host.Redraw();
        if (!pending) return;
        try
        {
            await task;
        }
        catch when (task.IsCanceled)
        {
            return;
        }
        Host.Redraw();
    }
}
