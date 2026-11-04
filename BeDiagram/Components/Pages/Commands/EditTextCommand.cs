namespace BeDiagram.Components.Pages.Commands;

/// <summary>Enter: edits the text of the selected item or connector.</summary>
public class EditTextCommand : EditorCommand
{
    public EditTextCommand() : base("Edit text", new KeyGesture("Enter")) { }

    protected override void Execute(ICommandContext ctx) => ctx.EditSelected();
}
