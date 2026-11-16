using BeDiagram.Models;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>Dragging the bottom-right handle of a shape, text box or group.</summary>
public class ResizeGesture : UndoableDragGesture
{
    readonly string id;
    readonly double w0, h0;

    public ResizeGesture(IEditorContext ctx, double x, double y, IBox box) : base(ctx, x, y)
    {
        id = box.Id;
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
        r.W = Math.Max(minW, Ctx.SnapV(w0 + dx));
        r.H = Math.Max(minH, Ctx.SnapV(h0 + dy));
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
