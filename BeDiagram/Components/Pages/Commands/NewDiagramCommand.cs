namespace BeDiagram.Components.Pages.Commands;

/// <summary>Alt+N: a new, empty diagram in a new tab (the browser keeps Ctrl+N for a new window).</summary>
public class NewDiagramCommand : EditorCommand
{
    public NewDiagramCommand() : base("New diagram (new tab)", new KeyGesture("n", Alt: true))
    {
    }

    protected override void Execute(ICommandContext ctx)
    {
        ctx.NewDiagram();
    }
}
