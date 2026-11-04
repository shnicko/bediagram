namespace BeDiagram.Components.Pages.Commands;

/// <summary>Arrow keys: move the selection by 10 px, or 1 px with Shift. One instance per direction and step.</summary>
public class NudgeCommand : EditorCommand
{
    readonly double dx, dy;

    /// <param name="key">"ArrowUp", "ArrowDown", "ArrowLeft" or "ArrowRight".</param>
    /// <param name="x">-1, 0 or 1.</param>
    /// <param name="y">-1, 0 or 1.</param>
    /// <param name="fine">Shift held: 1 px instead of 10.</param>
    public NudgeCommand(string key, int x, int y, bool fine)
        : base(fine ? "Nudge 1 px" : "Nudge", new KeyGesture(key, Shift: fine))
    {
        double step = fine ? 1 : 10;
        (dx, dy) = (x * step, y * step);
    }

    protected override void Execute(ICommandContext ctx) => ctx.Nudge(dx, dy);
}
