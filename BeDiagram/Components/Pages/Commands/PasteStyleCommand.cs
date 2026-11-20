namespace BeDiagram.Components.Pages.Commands;

/// <summary>Ctrl+Alt+V: paste the copied look onto the selected items of the same kind.</summary>
public class PasteStyleCommand : EditorCommand
{
    public PasteStyleCommand() : base("Paste style", new KeyGesture("v", Ctrl: true, Alt: true))
    {
    }

    public override bool CanExecute(ICommandContext ctx)
    {
        return ctx.HasCopiedStyle && ctx.HasSelection;
    }

    protected override void Execute(ICommandContext ctx)
    {
        ctx.PasteStyle();
    }
}
