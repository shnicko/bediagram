using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;

namespace BeDiagram.Components.Pages.Gestures;

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
        EdgeRoute route = Geometry.Route(Ctx.FindBox(edge.From)!, Ctx.FindBox(edge.To)!, edge);
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
