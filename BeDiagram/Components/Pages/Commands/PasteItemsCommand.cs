namespace BeDiagram.Components.Pages.Commands;

/// <summary>Ctrl+V: Paste the copied items (into this or any other tab).</summary>
public class PasteItemsCommand : EditorCommand
{
    public PasteItemsCommand() : base("Paste", new KeyGesture("v", Ctrl: true))
    {
    }

    public override bool CanExecute(ICommandContext ctx)
    {
        return ctx.HasCopiedItems;
    }

    protected override void Execute(ICommandContext ctx)
    {
        ctx.PasteItems();
    }
}
