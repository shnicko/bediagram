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
        (w0, h0) = (box.W, box.H);
    }

    protected override bool Drag(double dx, double dy)
    {
        if (Ctx.FindBox(id) is not { } r) return true;
        // A list / IPO node can't get shorter than its rows (they have a fixed height).
        (double minW, double minH) = r switch
        {
            Group => (160.0, 100.0),
            TextBox => (40.0, 24.0),
            Node n when n.Kind.IsPanel => (PanelGeometry.MinWidth(n), PanelGeometry.NeededHeight(n)),
            _ => (80.0, 40.0),
        };
        r.W = Math.Max(minW, Ctx.SnapV(w0 + dx));
        r.H = Math.Max(minH, Ctx.SnapV(h0 + dy));
        Moved = true;
        return true;
    }

    /// <summary>A bigger shape or group may no longer fit in the groups around it, so they grow.</summary>
    protected override void Finished()
    {
        if (Ctx.FindBox(id) is IGroupable item) Ctx.GrowAround(item);
    }
}
