namespace BeDiagram.Components.Pages.Commands;

/// <summary>Alt+PageUp: show the previous tab (before the first one, the last).</summary>
public class PreviousTabCommand : EditorCommand
{
    public PreviousTabCommand() : base("Previous tab", new KeyGesture("PageUp", Alt: true))
    {
    }

    public override bool CanExecute(ICommandContext ctx)
    {
        return ctx.TabCount > 1;
    }

    protected override void Execute(ICommandContext ctx)
    {
        ctx.StepTab(-1);
    }
}
