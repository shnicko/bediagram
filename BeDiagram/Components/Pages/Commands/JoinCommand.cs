namespace BeDiagram.Components.Pages.Commands;

/// <summary>Connects the selected items in the order they were picked.</summary>
public class JoinCommand : EditorCommand
{
    public JoinCommand() : base("Join in selection order", new KeyGesture("j")) { }

    protected override void Execute(ICommandContext ctx) => ctx.JoinSelected();
}
