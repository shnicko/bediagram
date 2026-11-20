namespace BeDiagram.Components.Pages.Commands;

/// <summary>Ctrl+X: Cut: copy the selected items, then delete them.</summary>
public class CutItemsCommand : EditorCommand
{
    public CutItemsCommand() : base("Cut", new KeyGesture("x", Ctrl: true))
    {
    }

    public override bool CanExecute(ICommandContext ctx)
    {
        return ctx.CanCopyItems;
    }

    protected override void Execute(ICommandContext ctx)
    {
        ctx.CutItems();
    }
}
