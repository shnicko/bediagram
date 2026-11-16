using BeDiagram.Models;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>
/// Which edges a resize handle moves. <paramref name="H"/>: -1 the left edge, 1 the right edge, 0 neither;
/// <paramref name="V"/>: -1 the top edge, 1 the bottom edge, 0 neither. The opposite edges stay where they are.
/// </summary>
public record ResizeHandle(int H, int V)
{
    /// <summary>The usual handle: bottom-right corner (shapes and text boxes only have this one).</summary>
    public static ResizeHandle BottomRight { get; } = new ResizeHandle(1, 1);

    /// <summary>The four corners of a group, which get a visible grip. Bottom-right comes first, as the one grip groups
    /// had before (the regression suites pick the first grip on the page).</summary>
    public static List<ResizeHandle> Corners()
    {
        List<ResizeHandle> result = new List<ResizeHandle>();
        result.Add(BottomRight);
        result.Add(new ResizeHandle(-1, 1));
        result.Add(new ResizeHandle(1, -1));
        result.Add(new ResizeHandle(-1, -1));
        return result;
    }

    /// <summary>The four sides of a group (top, right, bottom, left), dragged anywhere along the edge.</summary>
    public static List<ResizeHandle> Sides()
    {
        List<ResizeHandle> result = new List<ResizeHandle>();
        result.Add(new ResizeHandle(0, -1));
        result.Add(new ResizeHandle(1, 0));
        result.Add(new ResizeHandle(0, 1));
        result.Add(new ResizeHandle(-1, 0));
        return result;
    }

    /// <summary>CSS class (<c>resize-ns</c>, <c>resize-ew</c>, <c>resize-nwse</c>, <c>resize-nesw</c>) for the pointer cursor that matches the way this handle drags.</summary>
    public string CursorClass()
    {
        if (H == 0)
        {
            return "resize-ns";
        }
        if (V == 0)
        {
            return "resize-ew";
        }
        if (H == V)
        {
            return "resize-nwse";
        }
        return "resize-nesw";
    }

    /// <summary>The point of <paramref name="b"/> this handle sits on (a corner or the middle of a side).</summary>
    public (double X, double Y) On(IBox b)
    {
        double x = b.X + b.W * (H + 1) / 2;
        double y = b.Y + b.H * (V + 1) / 2;
        return (x, y);
    }
}
