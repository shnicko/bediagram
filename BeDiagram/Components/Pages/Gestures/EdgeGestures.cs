using BeDiagram.Models;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>Base for dragging a handle on the selected connector.</summary>
public abstract class EdgeHandleGesture : UndoableDragGesture
{
    protected EdgeHandleGesture(IEditorContext ctx, double x, double y, string edgeId) : base(ctx, x, y)
    {
        EdgeId = edgeId;
    }

    public string EdgeId { get; }

    protected Edge? Edge => Ctx.Diagram.Edges.FirstOrDefault(e => e.Id == EdgeId);

    /// <summary>
    /// The first time a connector is adjusted by hand, fix both ends to the sides they use now,
    /// so the change you make doesn't make the other end jump, and moving shapes later keeps your choice.
    /// </summary>
    protected void PinSides(Edge edge)
    {
        if (edge.Style == EdgeStyles.Straight) return;
        if (Ctx.FindBox(edge.From) is not { } a || Ctx.FindBox(edge.To) is not { } b) return;
        Geometry.EdgeRoute r = Geometry.Route(a, b, edge);
        edge.FromSide ??= r.FromSide;
        edge.ToSide ??= r.ToSide;
    }
}

/// <summary>Dragging a ● end of the selected connector to another side of its shape.</summary>
public class EdgeEndGesture : EdgeHandleGesture
{
    public EdgeEndGesture(IEditorContext ctx, double x, double y, string edgeId, bool fromEnd) : base(ctx, x, y, edgeId)
    {
        FromEnd = fromEnd;
    }

    /// <summary>True when dragging the start of the connector, false for its end. The canvas shows that shape's side targets.</summary>
    public bool FromEnd { get; }

    protected override bool Drag(double dx, double dy)
    {
        if (Edge is not { } edge) return true;
        IBox? box = Ctx.FindBox(FromEnd ? edge.From : edge.To);
        if (box is null) return true;
        // Nearest side middle to the pointer.
        string side = Sides.All.MinBy(s =>
        {
            (double px, double py) = Geometry.SidePoint(box, s);
            return (px - CurX) * (px - CurX) + (py - CurY) * (py - CurY);
        })!;
        Geometry.EdgeRoute route = Geometry.Route(Ctx.FindBox(edge.From)!, Ctx.FindBox(edge.To)!, edge);
        string? current = FromEnd ? route.FromSide : route.ToSide;
        if (side == current) return true;
        if (!Moved && Distance < 4) return true;
        if (!Moved) PinSides(edge);
        Moved = true;
        if (FromEnd) edge.FromSide = side; else edge.ToSide = side;
        edge.Bend = 0; // a new side means a new shape of route — start its bend from the middle again
        return true;
    }
}

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
        Geometry.EdgeRoute r = Geometry.Route(a, b, edge);
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
