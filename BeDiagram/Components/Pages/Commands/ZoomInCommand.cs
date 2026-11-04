namespace BeDiagram.Components.Pages.Commands;

/// <summary>Zooms in around the middle of the view.</summary>
public class ZoomInCommand : EditorCommand
{
    public ZoomInCommand() : base("Zoom in", new KeyGesture("+"), new KeyGesture("=")) { }

    protected override void Execute(ICommandContext ctx) => ctx.ZoomBy(1.2);
}
