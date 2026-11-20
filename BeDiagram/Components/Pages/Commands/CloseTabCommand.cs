namespace BeDiagram.Components.Pages.Commands;

/// <summary>Alt+W: close the tab on screen (asking first when it has unsaved changes).</summary>
public class CloseTabCommand : EditorCommand
{
    public CloseTabCommand() : base("Close tab", new KeyGesture("w", Alt: true))
    {
    }

    protected override void Execute(ICommandContext ctx)
    {
        ctx.CloseActiveTab();
    }
}
