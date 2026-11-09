using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Routing;

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
        if (Ctx.EdgeEnds(edge) is not (IBox a, IBox b)) return true;
        IBox box = FromEnd ? a : b;
        // Nearest side middle to the pointer (left / right only on a row of a list or IPO box).
        Side side = box.ConnectSides.MinBy(s =>
        {
            (double px, double py) = EdgeRouter.SidePoint(box, s);
            return (px - CurX) * (px - CurX) + (py - CurY) * (py - CurY);
        });
        EdgeRoute route = EdgeRouter.Route(a, b, edge);
        Side? current = FromEnd ? route.FromSide : route.ToSide;
        if (side == current) return true;
        if (!Moved && Distance < 4) return true;
        if (!Moved) PinSides(edge);
        Moved = true;
        if (FromEnd) edge.FromSide = side; else edge.ToSide = side;
        edge.Bend = 0; // a new side means a new shape of route — start its bend from the middle again
        return true;
    }
}
