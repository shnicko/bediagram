namespace BeDiagram.Components.Pages.Gestures;

/// <summary>
/// Base for gestures that work in world coordinates: remembers where the drag started and where the pointer is now,
/// and passes the offset to <see cref="Drag"/>.
/// </summary>
public abstract class DragGesture : IGesture
{
    protected DragGesture(IEditorContext ctx, double x, double y)
    {
        Ctx = ctx;
        StartX = CurX = x;
        StartY = CurY = y;
    }

    protected IEditorContext Ctx { get; }

    public double StartX { get; }
    public double StartY { get; }
    public double CurX { get; private set; }
    public double CurY { get; private set; }

    /// <summary>How far the pointer has travelled (Manhattan distance), used for click-vs-drag thresholds.</summary>
    protected double Distance => Math.Abs(CurX - StartX) + Math.Abs(CurY - StartY);

    public bool Move(GesturePoint p)
    {
        CurX = p.X;
        CurY = p.Y;
        return Drag(CurX - StartX, CurY - StartY);
    }

    /// <summary>Apply the drag offset from the start point. Return false when nothing visible changed.</summary>
    protected abstract bool Drag(double dx, double dy);

    public abstract void Up();

    public virtual void Cancel() { }
}

/// <summary>
/// A drag that edits the diagram: takes an undo snapshot when it starts and records it on pointer-up,
/// but only if the drag actually changed something (<see cref="Moved"/>), so a plain click adds no undo step.
/// </summary>
public abstract class UndoableDragGesture : DragGesture
{
    readonly string snapshot;

    protected UndoableDragGesture(IEditorContext ctx, double x, double y) : base(ctx, x, y)
    {
        snapshot = ctx.Snapshot();
    }

    protected bool Moved { get; set; }

    public override void Up()
    {
        if (!Moved) return;
        Ctx.PushUndo(snapshot);
        Ctx.MarkDirty();
        Finished();
    }

    /// <summary>Extra work after a drag that changed something (e.g. update group membership).</summary>
    protected virtual void Finished() { }
}
