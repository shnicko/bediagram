namespace BeDiagram.Components.Pages.Commands;

/// <summary>Groups the selection, or adds an empty group in the middle of the view.</summary>
public class AddGroupCommand : EditorCommand
{
    public AddGroupCommand() : base("Add group", new KeyGesture("g")) { }

    protected override void Execute(ICommandContext ctx) => ctx.AddGroup();
}
