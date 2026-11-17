using BeDiagram.Models;

namespace BeDiagram.Editing.Guides;

/// <summary>
/// The centre lines a dragged item can line up with, like PowerPoint's smart guides but only through centres (no
/// edges or equal spacing yet): a vertical and a horizontal ray through the centre of every shape that isn't moving.
/// Text boxes and groups don't give guides. The dragged item lines up its own snap point
/// (<see cref="Models.Location.IPlacement.SnapPoint"/>): a shape's or group's centre, a text box's top centre.
/// </summary>
public class CentreGuides
{
    readonly List<(double X, double Y)> centres = new List<(double X, double Y)>();

    /// <param name="moving">Ids of everything being dragged: they don't give guides.</param>
    public CentreGuides(Diagram d, HashSet<string> moving)
    {
        foreach (Node n in d.Nodes)
        {
            if (moving.Contains(n.Id))
            {
                continue;
            }
            centres.Add(n.Placement.SnapPoint(n, n.X, n.Y));
        }
    }

    /// <summary>The x of the shape centre nearest to <paramref name="x"/>, if one is within <paramref name="reach"/>.</summary>
    public double? NearestX(double x, double reach)
    {
        double? best = null;
        double bestGap = 0;
        foreach ((double cx, double cy) in centres)
        {
            double gap = Math.Abs(cx - x);
            if (gap > reach)
            {
                continue;
            }
            if (best is null || gap < bestGap)
            {
                best = cx;
                bestGap = gap;
            }
        }
        return best;
    }

    /// <summary>The y of the shape centre nearest to <paramref name="y"/>, if one is within <paramref name="reach"/>.</summary>
    public double? NearestY(double y, double reach)
    {
        double? best = null;
        double bestGap = 0;
        foreach ((double cx, double cy) in centres)
        {
            double gap = Math.Abs(cy - y);
            if (gap > reach)
            {
                continue;
            }
            if (best is null || gap < bestGap)
            {
                best = cy;
                bestGap = gap;
            }
        }
        return best;
    }

    /// <summary>
    /// The lines to draw for the guides the dragged item is on: a vertical line at <paramref name="guideX"/> and / or a
    /// horizontal one at <paramref name="guideY"/>, each running from the item's snap point (<paramref name="x"/>,
    /// <paramref name="y"/>) to the furthest shape centre on that line.
    /// </summary>
    public List<GuideLine> Lines(double x, double y, double? guideX, double? guideY)
    {
        List<GuideLine> result = new List<GuideLine>();
        if (guideX is not null)
        {
            double top = y;
            double bottom = y;
            foreach ((double cx, double cy) in centres)
            {
                if (cx == guideX.Value)
                {
                    top = Math.Min(top, cy);
                    bottom = Math.Max(bottom, cy);
                }
            }
            result.Add(new GuideLine(guideX.Value, top, guideX.Value, bottom));
        }
        if (guideY is not null)
        {
            double left = x;
            double right = x;
            foreach ((double cx, double cy) in centres)
            {
                if (cy == guideY.Value)
                {
                    left = Math.Min(left, cx);
                    right = Math.Max(right, cx);
                }
            }
            result.Add(new GuideLine(left, guideY.Value, right, guideY.Value));
        }
        return result;
    }
}
