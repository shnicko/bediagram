namespace BeDiagram.Components.Pages.Commands;

/// <summary>Deletes the selection (a group with its contents).</summary>
public class DeleteCommand : EditorCommand
{
    public DeleteCommand() : base("Delete", new KeyGesture("Delete"), new KeyGesture("Backspace")) { }

    protected override void Execute(ICommandContext ctx) => ctx.DeleteSelection();
}
