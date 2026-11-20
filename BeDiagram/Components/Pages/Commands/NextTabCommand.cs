namespace BeDiagram.Components.Pages.Commands;

/// <summary>Alt+PageDown: show the next tab (after the last one, the first).</summary>
public class NextTabCommand : EditorCommand
{
    public NextTabCommand() : base("Next tab", new KeyGesture("PageDown", Alt: true))
    {
    }

    public override bool CanExecute(ICommandContext ctx)
    {
        return ctx.TabCount > 1;
    }

    protected override void Execute(ICommandContext ctx)
    {
        ctx.StepTab(1);
    }
}
