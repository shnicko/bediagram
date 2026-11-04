namespace BeDiagram.Components.Pages.Commands;

/// <summary>Adds a text box (joined to the selection when Auto-join is on).</summary>
public class AddTextCommand : EditorCommand
{
    public AddTextCommand() : base("Add text box", new KeyGesture("t")) { }

    protected override void Execute(ICommandContext ctx) => ctx.AddText();
}
