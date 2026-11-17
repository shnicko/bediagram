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
        origins = new Dictionary<string, (double X, double Y)>();
        foreach (IBox b in items)
        {
            origins.Add(b.Id, (b.X, b.Y));
        }
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
            // Snap centres (not corners) so shapes of different sizes line up and connectors stay straight.
            if (o.SnapX)
            {
                b.X = Ctx.SnapV(ox + o.Dx + b.W / 2) - b.W / 2;
            }
            else
            {
                b.X = ox + o.Dx;
            }
            if (o.SnapY)
            {
                b.Y = Ctx.SnapV(oy + o.Dy + b.H / 2) - b.H / 2;
            }
            else
            {
                b.Y = oy + o.Dy;
            }
        }
        return true;
    }

    protected override void Finished()
    {
        // Items dragged on their own (not carried by a group that moved with them) may have entered or left a group.
        // Groups count too: a group dropped wholly inside another group nests in it.
        List<IGroupable> loose = new List<IGroupable>();
        foreach (string id in origins.Keys)
        {
            IBox? b = Ctx.FindBox(id);
            if (b is IGroupable i)
            {
                if (i.GroupId is null || !origins.ContainsKey(i.GroupId))
                {
                    loose.Add(i);
                }
            }
        }
        Ctx.UpdateMembership(loose);
    }
}
