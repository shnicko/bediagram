namespace BeDiagram.Components.Pages.Gestures;

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
