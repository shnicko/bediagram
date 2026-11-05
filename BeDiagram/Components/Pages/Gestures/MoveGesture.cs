using BeDiagram.Editing.Angles;
using BeDiagram.Models;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>
/// Dragging the selection (groups bring their members). Snaps shape centres to the grid, and keeps the move on an
/// allowed angle when an angle lock is on (an axis the lock holds still is not snapped, so items stay in line).
/// </summary>
public class MoveGesture : UndoableDragGesture
{
    readonly Dictionary<string, (double X, double Y)> origins;

    /// <param name="items">Everything that moves: the selection plus members of selected groups.</param>
    public MoveGesture(IEditorContext ctx, double x, double y, IEnumerable<IBox> items) : base(ctx, x, y)
    {
        origins = items.ToDictionary(b => b.Id, b => (b.X, b.Y));
    }

    protected override bool Drag(double dx, double dy)
    {
        if (!Moved && Math.Abs(dx) + Math.Abs(dy) < 2) return false;
        Moved = true;
        LockedOffset o = Ctx.LockOffset(dx, dy);
        foreach ((string id, (double ox, double oy)) in origins)
        {
            IBox? b = Ctx.FindBox(id);
            if (b is null) continue;
            // Snap centres (not corners) so shapes of different sizes line up and connectors stay straight.
            b.X = o.SnapX ? Ctx.SnapV(ox + o.Dx + b.W / 2) - b.W / 2 : ox + o.Dx;
            b.Y = o.SnapY ? Ctx.SnapV(oy + o.Dy + b.H / 2) - b.H / 2 : oy + o.Dy;
        }
        return true;
    }

    protected override void Finished()
    {
        // Items dragged on their own (not carried by a group that moved with them) may have entered or left a group.
        // Groups count too: a group dropped wholly inside another group nests in it.
        List<IGroupable> loose = origins.Keys.Select(Ctx.FindBox).OfType<IGroupable>()
            .Where(i => i.GroupId is null || !origins.ContainsKey(i.GroupId)).ToList();
        Ctx.UpdateMembership(loose);
    }
}
