using BeDiagram.Editing.Lanes;
using BeDiagram.Models;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>
/// Dragging the edge two touching lanes share (<see cref="LaneDivider"/>): the edge follows the pointer (snapped to the
/// grid), so one lane gets bigger as the other gets smaller and they stay edge to edge. Their far edges and everything
/// inside both lanes stay where they are. Neither lane goes below the smallest group size.
/// </summary>
public class LaneDividerGesture : UndoableDragGesture
{
    readonly string beforeId, afterId;
    readonly bool rows;
    /// <summary>Where the shared edge started, and the two lanes' far edges (which don't move).</summary>
    readonly double at0, beforeStart, afterEnd;

    public LaneDividerGesture(IEditorContext ctx, double x, double y, LaneDivider d) : base(ctx, x, y)
    {
        beforeId = d.Before.Id;
        afterId = d.After.Id;
        rows = d.Rows;
        at0 = d.At;
        if (rows)
        {
            beforeStart = d.Before.Y;
            afterEnd = d.After.Y + d.After.H;
        }
        else
        {
            beforeStart = d.Before.X;
            afterEnd = d.After.X + d.After.W;
        }
    }

    protected override bool Drag(double dx, double dy)
    {
        Group? before = Ctx.FindGroup(beforeId);
        Group? after = Ctx.FindGroup(afterId);
        if (before is null || after is null)
        {
            return true;
        }
        double move = dx;
        double min = BoxSizes.GroupMinW;
        if (rows)
        {
            move = dy;
            min = BoxSizes.GroupMinH;
        }
        if (!Moved && Math.Abs(move) < 2)
        {
            return false;
        }
        // The new edge, kept far enough from both far edges that neither lane gets too small.
        double at = Ctx.SnapV(at0 + move);
        at = Math.Max(at, beforeStart + min);
        at = Math.Min(at, afterEnd - min);
        if (rows)
        {
            before.H = at - beforeStart;
            after.Y = at;
            after.H = afterEnd - at;
        }
        else
        {
            before.W = at - beforeStart;
            after.X = at;
            after.W = afterEnd - at;
        }
        Moved = true;
        return true;
    }
}
