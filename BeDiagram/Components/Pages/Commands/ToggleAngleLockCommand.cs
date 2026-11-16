namespace BeDiagram.Components.Pages.Commands;

/// <summary>Switches the angle lock between Free and the last lock picked (90°, 45° or 30°).</summary>
public class ToggleAngleLockCommand : EditorCommand
{
    public ToggleAngleLockCommand() : base("Angle lock: free / last lock", new KeyGesture("a"))
    {
    }

    protected override void Execute(ICommandContext ctx)
    {
        ctx.ToggleAngleLock();
    }
}
