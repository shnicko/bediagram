namespace BeDiagram.Components.Pages.Commands;

/// <summary>Escape: stops editing and any drag, and clears the selection.</summary>
public class CancelCommand : EditorCommand
{
    public CancelCommand() : base("Cancel", new KeyGesture("Escape")) { }

    protected override void Execute(ICommandContext ctx) => ctx.CancelAndClearSelection();
}
