namespace BeDiagram.Components.Pages.Commands;

/// <summary>Zooms to show the whole drawing (or the whole page).</summary>
public class FitCommand : EditorCommand
{
    public FitCommand() : base("Fit to content", new KeyGesture("f")) { }

    protected override void Execute(ICommandContext ctx) => ctx.Fit();
}
