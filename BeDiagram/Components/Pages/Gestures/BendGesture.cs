using BeDiagram.Models;
using BeDiagram.Models.Layout;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>Dragging the ■ on an elbow connector's middle segment sideways.</summary>
public class BendGesture : EdgeHandleGesture
{
    readonly bool vertical;
    readonly double coord0, bend0;

    BendGesture(IEditorContext ctx, double x, double y, Edge edge, bool vertical, double coord0) : base(ctx, x, y, edge.Id)
    {
        this.vertical = vertical;
        this.coord0 = coord0;
        bend0 = edge.Bend;
    }

    /// <summary>Starts a bend drag, or returns null when the connector has no middle segment to drag.</summary>
    public static BendGesture? TryStart(IEditorContext ctx, double x, double y, string edgeId)
    {
        Edge? edge = ctx.Diagram.Edges.FirstOrDefault(e => e.Id == edgeId);
        if (edge is null || ctx.FindBox(edge.From) is not { } a || ctx.FindBox(edge.To) is not { } b) return null;
        EdgeRoute r = Geometry.Route(a, b, edge);
        if (r.BendIndex < 0) return null;
        bool vertical = r.BendIsVertical;
        double coord0 = vertical ? r.Points[r.BendIndex].X : r.Points[r.BendIndex].Y;
        return new BendGesture(ctx, x, y, edge, vertical, coord0);
    }

    protected override bool Drag(double dx, double dy)
    {
        if (Edge is not { } edge) return true;
        double d = vertical ? dx : dy;
        if (!Moved && Math.Abs(d) < 4) return true;
        if (!Moved) PinSides(edge);
        Moved = true;
        // Snap the segment itself (not the offset) to the grid.
        edge.Bend = bend0 + Ctx.SnapV(coord0 + d) - coord0;
        return true;
    }
}
