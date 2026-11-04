namespace BeDiagram.Components.Pages.Commands;

/// <summary>Sets the zoom back to 100%.</summary>
public class ResetZoomCommand : EditorCommand
{
    public ResetZoomCommand() : base("Zoom to 100%", new KeyGesture("0")) { }

    protected override void Execute(ICommandContext ctx) => ctx.ResetZoom();
}
