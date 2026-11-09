using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;

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
        if (edge.Style == EdgeStyle.Straight) return;
        if (Ctx.EdgeEnds(edge) is not (IBox a, IBox b)) return;
        EdgeRoute r = Geometry.Route(a, b, edge);
        edge.FromSide ??= r.FromSide;
        edge.ToSide ??= r.ToSide;
    }
}
