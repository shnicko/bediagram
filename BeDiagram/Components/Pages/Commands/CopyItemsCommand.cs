namespace BeDiagram.Components.Pages.Commands;

/// <summary>Ctrl+C: Copy the selected shapes, text boxes and groups (with the connectors between them), to paste in any tab.</summary>
public class CopyItemsCommand : EditorCommand
{
    public CopyItemsCommand() : base("Copy", new KeyGesture("c", Ctrl: true))
    {
    }

    public override bool CanExecute(ICommandContext ctx)
    {
        return ctx.CanCopyItems;
    }

    protected override void Execute(ICommandContext ctx)
    {
        ctx.CopyItems();
    }
}
