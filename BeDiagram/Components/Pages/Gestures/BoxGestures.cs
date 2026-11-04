using BeDiagram.Models;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>Dragging the selection (groups bring their members). Snaps shape centres to the grid.</summary>
public class MoveGesture : UndoableDragGesture
{
    readonly Dictionary<string, (double X, double Y)> origins;

    /// <param name="items">Everything that moves: the selection plus members of selected groups.</param>
    public MoveGesture(IEditorContext ctx, double x, double y, IEnumerable<IBox> items) : base(ctx, x, y)
    {
        origins = items.ToDictionary(b => b.Id, b => (b.X, b.Y));
    }

    protected override bool Drag(double dx, double dy)
    {
        if (!Moved && Math.Abs(dx) + Math.Abs(dy) < 2) return false;
        Moved = true;
        foreach ((string id, (double ox, double oy)) in origins)
        {
            IBox? b = Ctx.FindBox(id);
            if (b is null) continue;
            // Snap centres (not corners) so shapes of different sizes line up and connectors stay straight.
            b.X = Ctx.SnapV(ox + dx + b.W / 2) - b.W / 2;
            b.Y = Ctx.SnapV(oy + dy + b.H / 2) - b.H / 2;
        }
        return true;
    }

    protected override void Finished()
    {
        // Items dragged on their own may have entered or left a group.
        HashSet<string> movedGroups = Ctx.Selection.Where(id => Ctx.FindGroup(id) is not null).ToHashSet();
        List<IGroupable> loose = origins.Keys.Select(Ctx.FindBox).OfType<IGroupable>()
            .Where(i => i.GroupId is null || !movedGroups.Contains(i.GroupId)).ToList();
        Ctx.UpdateMembership(loose);
    }
}

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
        (double minW, double minH) = r switch { Group => (160.0, 100.0), TextBox => (40.0, 24.0), _ => (80.0, 40.0) };
        r.W = Math.Max(minW, Ctx.SnapV(w0 + dx));
        r.H = Math.Max(minH, Ctx.SnapV(h0 + dy));
        Moved = true;
        return true;
    }
}

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

/// <summary>
/// Selection box. Drag adds everything it touches (groups only when fully inside).
/// A click on a group's body (no drag) selects that group instead.
/// </summary>
public class MarqueeGesture : DragGesture
{
    readonly string? clickSelects;

    /// <param name="clickSelects">Group to select if this turns out to be a click, not a drag.</param>
    public MarqueeGesture(IEditorContext ctx, double x, double y, string? clickSelects = null) : base(ctx, x, y)
    {
        this.clickSelects = clickSelects;
    }

    // The box as drawn on the canvas.
    public double X => Math.Min(StartX, CurX);
    public double Y => Math.Min(StartY, CurY);
    public double W => Math.Abs(CurX - StartX);
    public double H => Math.Abs(CurY - StartY);

    protected override bool Drag(double dx, double dy) => true;

    public override void Up()
    {
        if (Distance > 4)
        {
            double x1 = X, y1 = Y, x2 = X + W, y2 = Y + H;
            Diagram d = Ctx.Diagram;
            IOrderedEnumerable<IBox> hits = d.Nodes.Cast<IBox>().Concat(d.TextBoxes)
                .Where(b => Geometry.Intersects(b, x1, y1, x2, y2))
                // Groups only when fully enclosed, so dragging inside a group selects its contents.
                .Concat(d.Groups.Where(g => g.X >= x1 && g.Y >= y1 && g.X + g.W <= x2 && g.Y + g.H <= y2))
                .OrderBy(b => b.Y).ThenBy(b => b.X);
            foreach (IBox b in hits)
                if (!Ctx.Selection.Contains(b.Id)) Ctx.Selection.Add(b.Id);
        }
        else if (clickSelects is not null)
        {
            Ctx.SelectOnly(clickSelects);
        }
    }
}

/// <summary>Right/middle-button drag: moves the view. Works in screen px, not world coordinates.</summary>
public class PanGesture : IGesture
{
    readonly IEditorContext ctx;
    double lastX, lastY;

    public PanGesture(IEditorContext ctx, double clientX, double clientY)
    {
        this.ctx = ctx;
        (lastX, lastY) = (clientX, clientY);
    }

    public bool Move(GesturePoint p)
    {
        ctx.PanBy(p.ClientX - lastX, p.ClientY - lastY);
        (lastX, lastY) = (p.ClientX, p.ClientY);
        return true;
    }

    public void Up() { }

    public void Cancel() { }
}
