namespace BeDiagram.Components.Pages.Commands;

/// <summary>Ctrl+Alt+C: copy the look of the selected item or connector, to paste onto others (in any tab).
/// Ctrl+Shift+C would be the Office key, but the browser keeps it for its developer tools.</summary>
public class CopyStyleCommand : EditorCommand
{
    public CopyStyleCommand() : base("Copy style", new KeyGesture("c", Ctrl: true, Alt: true))
    {
    }

    public override bool CanExecute(ICommandContext ctx)
    {
        return ctx.CanCopyStyle;
    }

    protected override void Execute(ICommandContext ctx)
    {
        ctx.CopyStyle();
    }
}
