using BeDiagram.Editing;
using BeDiagram.Editing.Angles;
using BeDiagram.Models;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>
/// Dragging the selection (groups bring their members: <see cref="MoveSet"/>). Each item snaps to the grid the way
/// its placement says (<see cref="Models.Location.IPlacement.SnapTopLeft"/>: its centre, for now), and the move keeps
/// to an allowed angle when an angle lock is on (an axis the lock holds still is not snapped, so items stay in line).
/// </summary>
public class MoveGesture : UndoableDragGesture
{
    readonly Dictionary<string, (double X, double Y)> origins;
    readonly List<IGroupable> roots;

    /// <param name="moving">What the drag moves (<see cref="DiagramEditor.MoveSetFor"/>).</param>
    public MoveGesture(IEditorContext ctx, double x, double y, MoveSet moving) : base(ctx, x, y)
    {
        origins = new Dictionary<string, (double X, double Y)>();
        foreach (IBox b in moving.Items)
        {
            origins.Add(b.Id, (b.X, b.Y));
        }
        roots = moving.Roots;
    }

    protected override bool Drag(double dx, double dy)
    {
        if (!Moved && Math.Abs(dx) + Math.Abs(dy) < 2)
        {
            return false;
        }
        Moved = true;
        LockedOffset o = Ctx.LockOffset(dx, dy);
        foreach ((string id, (double ox, double oy)) in origins)
        {
            IBox? b = Ctx.FindBox(id);
            if (b is null)
            {
                continue;
            }
            // Every item moves from where it started, so the angle lock can hold an axis still; members of a dragged
            // group snap on their own (group carry while dragging waits for the snapping review).
            (double x, double y) = b.Placement.SnapTopLeft(b, ox + o.Dx, oy + o.Dy, o.SnapX, o.SnapY, Ctx.SnapV);
            b.X = x;
            b.Y = y;
        }
        return true;
    }

    protected override void Finished()
    {
        // The roots (items not carried by a group that moved with them) may have entered or left a group. Groups count
        // too: a group dropped wholly inside another group nests in it.
        Ctx.UpdateMembership(roots);
    }
}
