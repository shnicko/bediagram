using BeDiagram.Models;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>
/// Dragging a shape's free text (<see cref="Node.FreeLabel"/>: a label below a circle or outcome, and the description)
/// away from the shape. Only the offset changes, so the shape stays put and the text still moves with it later.
/// </summary>
public class FreeTextGesture : UndoableDragGesture
{
    readonly string id;
    readonly Offset start;

    public FreeTextGesture(IEditorContext ctx, double x, double y, Node n) : base(ctx, x, y)
    {
        id = n.Id;
        start = n.FreeLabel ?? new Offset(0, 0);
    }

    protected override bool Drag(double dx, double dy)
    {
        if (Ctx.FindBox(id) is not Node n)
        {
            return false;
        }
        // Whole pixels keep the saved file tidy.
        n.FreeLabel = new Offset(Math.Round(start.X + dx), Math.Round(start.Y + dy));
        Moved = true;
        return true;
    }

    /// <summary>The text may now reach outside the shape's groups, so they grow.</summary>
    protected override void Finished()
    {
        if (Ctx.FindBox(id) is IGroupable item)
        {
            Ctx.GrowAround(item);
        }
    }
}
