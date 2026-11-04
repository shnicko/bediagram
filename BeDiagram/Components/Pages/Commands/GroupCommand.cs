namespace BeDiagram.Components.Pages.Commands;

/// <summary>Puts the selected shapes and text boxes in a new group.</summary>
public class GroupCommand : EditorCommand
{
    public GroupCommand() : base("Group", new KeyGesture("g", Ctrl: true)) { }

    protected override void Execute(ICommandContext ctx) => ctx.GroupSelection();
}
