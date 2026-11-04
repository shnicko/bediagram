using BeDiagram.Models;

namespace BeDiagram.Components.Pages.Gestures;

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
