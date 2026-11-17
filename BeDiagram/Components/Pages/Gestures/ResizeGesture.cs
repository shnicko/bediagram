using BeDiagram.Models;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>
/// Dragging a resize handle: the bottom-right one of a shape or text box, or any corner or side of a group
/// (<see cref="ResizeHandle"/>). The edges the handle doesn't move stay where they are, and nothing inside a group
/// moves, so a group can be made bigger upwards or to the left without carrying its shapes. With Alt held on a lane
/// (a top-level group), every other lane whose moving edges line up with it gets the same change, so lanes stacked
/// edge to edge can be lengthened together.
/// </summary>
public class ResizeGesture : UndoableDragGesture
{
    /// <summary>A box being resized and its size and place when the drag started.</summary>
    sealed class Start
    {
        public required string Id { get; init; }
        public required double X { get; init; }
        public required double Y { get; init; }
        public required double W { get; init; }
        public required double H { get; init; }
    }

    /// <summary>How close (px) another lane's edge must be to count as lined up.</summary>
    const double LinedUp = 0.5;

    readonly ResizeHandle handle;
    /// <summary>The box the handle belongs to first, then any lanes lined up with it.</summary>
    readonly List<Start> boxes = new List<Start>();

    /// <param name="together">Alt was held: resize the lanes lined up with this one too.</param>
    public ResizeGesture(IEditorContext ctx, double x, double y, IBox box, ResizeHandle handle, bool together) : base(ctx, x, y)
    {
        this.handle = handle;
        boxes.Add(StartOf(box));
        if (together && box is Group lane && lane.GroupId is null)
        {
            foreach (Group other in ctx.Diagram.Groups)
            {
                if (other != lane && other.GroupId is null && LinesUp(lane, other))
                {
                    boxes.Add(StartOf(other));
                }
            }
        }
    }

    static Start StartOf(IBox b)
    {
        return new Start { Id = b.Id, X = b.X, Y = b.Y, W = b.W, H = b.H };
    }

    /// <summary>True when every edge this handle moves sits in the same place on both lanes.</summary>
    bool LinesUp(Group a, Group b)
    {
        if (handle.H == 1 && Math.Abs(a.X + a.W - (b.X + b.W)) > LinedUp)
        {
            return false;
        }
        if (handle.H == -1 && Math.Abs(a.X - b.X) > LinedUp)
        {
            return false;
        }
        if (handle.V == 1 && Math.Abs(a.Y + a.H - (b.Y + b.H)) > LinedUp)
        {
            return false;
        }
        if (handle.V == -1 && Math.Abs(a.Y - b.Y) > LinedUp)
        {
            return false;
        }
        return true;
    }

    protected override bool Drag(double dx, double dy)
    {
        foreach (Start s in boxes)
        {
            IBox? r = Ctx.FindBox(s.Id);
            if (r is not null)
            {
                Resize(r, s, dx, dy);
            }
        }
        Moved = true;
        return true;
    }

    /// <summary>Moves the edges this handle drags by (dx, dy) from where they started, snapped, not below the box's
    /// smallest size; the other edges stay put.</summary>
    void Resize(IBox r, Start s, double dx, double dy)
    {
        // A list / IPO node can't get shorter than its rows (they have a fixed height).
        (double minW, double minH) = r.Placement.MinSize(r);

        // Across: the right edge follows the pointer, or the left edge does and the right edge stays put.
        if (handle.H == 1)
        {
            r.W = Math.Max(minW, Ctx.SnapV(s.W + dx));
        }
        else if (handle.H == -1)
        {
            r.W = Math.Max(minW, Ctx.SnapV(s.W - dx));
            r.X = s.X + s.W - r.W;
        }

        // Down: the same for the bottom and top edges.
        if (handle.V == 1)
        {
            r.H = Math.Max(minH, Ctx.SnapV(s.H + dy));
        }
        else if (handle.V == -1)
        {
            r.H = Math.Max(minH, Ctx.SnapV(s.H - dy));
            r.Y = s.Y + s.H - r.H;
        }
    }

    /// <summary>A bigger shape or group may no longer fit in the groups around it, so they grow.</summary>
    protected override void Finished()
    {
        foreach (Start s in boxes)
        {
            if (Ctx.FindBox(s.Id) is IGroupable item)
            {
                Ctx.GrowAround(item);
            }
        }
    }
}
