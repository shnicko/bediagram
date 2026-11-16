using BeDiagram.Models;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>
/// Dragging a resize handle: the bottom-right one of a shape or text box, or any corner or side of a group
/// (<see cref="ResizeHandle"/>). The edges the handle doesn't move stay where they are, and nothing inside a group
/// moves, so a group can be made bigger upwards or to the left without carrying its shapes.
/// </summary>
public class ResizeGesture : UndoableDragGesture
{
    readonly string id;
    readonly ResizeHandle handle;
    readonly double x0, y0, w0, h0;

    public ResizeGesture(IEditorContext ctx, double x, double y, IBox box, ResizeHandle handle) : base(ctx, x, y)
    {
        id = box.Id;
        this.handle = handle;
        x0 = box.X;
        y0 = box.Y;
        w0 = box.W;
        h0 = box.H;
    }

    protected override bool Drag(double dx, double dy)
    {
        IBox? r = Ctx.FindBox(id);
        if (r is null)
        {
            return true;
        }
        // A list / IPO node can't get shorter than its rows (they have a fixed height).
        double minW = 80;
        double minH = 40;
        if (r is Group)
        {
            minW = 160;
            minH = 100;
        }
        else if (r is TextBox)
        {
            minW = 40;
            minH = 24;
        }
        else if (r is Node n)
        {
            (minW, minH) = n.Kind.MinSize(n);
        }

        // Across: the right edge follows the pointer, or the left edge does and the right edge stays put.
        if (handle.H == 1)
        {
            r.W = Math.Max(minW, Ctx.SnapV(w0 + dx));
        }
        else if (handle.H == -1)
        {
            r.W = Math.Max(minW, Ctx.SnapV(w0 - dx));
            r.X = x0 + w0 - r.W;
        }

        // Down: the same for the bottom and top edges.
        if (handle.V == 1)
        {
            r.H = Math.Max(minH, Ctx.SnapV(h0 + dy));
        }
        else if (handle.V == -1)
        {
            r.H = Math.Max(minH, Ctx.SnapV(h0 - dy));
            r.Y = y0 + h0 - r.H;
        }
        Moved = true;
        return true;
    }

    /// <summary>A bigger shape or group may no longer fit in the groups around it, so they grow.</summary>
    protected override void Finished()
    {
        if (Ctx.FindBox(id) is IGroupable item)
        {
            Ctx.GrowAround(item);
        }
    }
}
