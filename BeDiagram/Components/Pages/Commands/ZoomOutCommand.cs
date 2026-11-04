namespace BeDiagram.Components.Pages.Commands;

/// <summary>Zooms out around the middle of the view.</summary>
public class ZoomOutCommand : EditorCommand
{
    public ZoomOutCommand() : base("Zoom out", new KeyGesture("-")) { }

    protected override void Execute(ICommandContext ctx) => ctx.ZoomBy(1 / 1.2);
}
