using BeDiagram.Models;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>
/// Dragging from a shape's port. Drop on another item to connect; drop on empty canvas (far enough away)
/// to create a new joined step there. The canvas draws a live preview from <see cref="FromId"/> to the pointer.
/// </summary>
public class ConnectGesture : DragGesture
{
    public ConnectGesture(IEditorContext ctx, double x, double y, string fromId) : base(ctx, x, y)
    {
        FromId = fromId;
    }

    public string FromId { get; }

    /// <summary>The item the connector would join to if released now (null over empty canvas).</summary>
    public IBox? Target => Ctx.HitTest(CurX, CurY, FromId);

    protected override bool Drag(double dx, double dy) => true;

    public override void Up()
    {
        if (Target is { } target)
        {
            string snap = Ctx.Snapshot();
            if (Ctx.AddEdge(FromId, target.Id)) { Ctx.PushUndo(snap); Ctx.MarkDirty(); }
        }
        else if (Distance > 40)
        {
            // Dropped on empty space: create a new step there, joined to the source (selected when the drag began).
            Ctx.AddNodeAt(NodeTypes.Process, CurX, CurY, join: true);
        }
    }
}
