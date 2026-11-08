using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>
/// Dragging from a shape's port (or a row port of a list / IPO node). Drop on another item to connect — on a row of a
/// list or IPO box to connect to that row; drop on empty canvas (far enough away) to create a new joined step there
/// (on an allowed angle when an angle lock is on). The canvas draws a live preview from <see cref="From"/> to the pointer.
/// </summary>
public class ConnectGesture : DragGesture
{
    public ConnectGesture(IEditorContext ctx, double x, double y, string fromId, string? fromItem = null) : base(ctx, x, y)
    {
        FromId = fromId;
        FromItem = fromItem;
    }

    public string FromId { get; }

    /// <summary>The row the connector starts from, or null for the whole shape.</summary>
    public string? FromItem { get; }

    /// <summary>What the connector starts from: the row, or the shape (null when it has gone).</summary>
    public IBox? From => Ctx.FindBox(FromId) is { } box ? PanelGeometry.End(box, FromItem) : null;

    /// <summary>What the connector would join to if released now: a row of a list / IPO node when over one
    /// (an <see cref="ItemBox"/>), the item under the pointer, or null over empty canvas.</summary>
    public IBox? Target =>
        Ctx.HitTest(CurX, CurY, FromId) is { } hit
            ? (hit is Node n && PanelGeometry.RowAt(n, CurX, CurY) is { } row ? row : hit)
            : null;

    /// <summary>
    /// Where a new step would be centred if released over empty canvas: on an allowed angle from the source's centre
    /// (angle lock), or the pointer itself when the source has gone.
    /// </summary>
    public (double X, double Y) DropPoint =>
        From is { } from ? Ctx.LockPoint(from.X + from.W / 2, from.Y + from.H / 2, CurX, CurY) : (CurX, CurY);

    protected override bool Drag(double dx, double dy) => true;

    public override void Up()
    {
        if (Target is { } target)
        {
            string snap = Ctx.Snapshot();
            if (Ctx.AddEdge(FromId, target.Id, FromItem, (target as ItemBox)?.ItemId)) { Ctx.PushUndo(snap); Ctx.MarkDirty(); }
        }
        else if (Distance > 40)
        {
            // Dropped on empty space: create a new step there, joined to the source (selected when the drag began).
            int before = Ctx.Diagram.Edges.Count;
            Ctx.AddNodeAt(NodeTypes.Process, CurX, CurY, join: true, lockAngle: true);
            // Started from a row: the new joining connector leaves from that row.
            if (FromItem is not null && Ctx.Diagram.Edges.Count > before && Ctx.Diagram.Edges[^1] is { } joined && joined.From == FromId)
                joined.FromItem = FromItem;
        }
    }
}
