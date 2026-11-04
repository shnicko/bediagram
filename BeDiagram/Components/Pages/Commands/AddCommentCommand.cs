namespace BeDiagram.Components.Pages.Commands;

/// <summary>Adds a comment pinned to the selection.</summary>
public class AddCommentCommand : EditorCommand
{
    public AddCommentCommand() : base("Add comment", new KeyGesture("c")) { }

    protected override void Execute(ICommandContext ctx) => ctx.AddComment();
}
