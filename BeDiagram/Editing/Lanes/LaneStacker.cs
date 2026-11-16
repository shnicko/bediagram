using BeDiagram.Models;

namespace BeDiagram.Editing.Lanes;

/// <summary>
/// Stack lanes edge to edge: closes the gaps and overlaps between lanes without changing their size or their order.
/// Lanes sitting one above another become rows, side by side become columns (whichever way their centres are more
/// spread out, as <see cref="LaneArranger"/> decides the order). The first lane stays where it is; the others line up
/// with its start along the flow and follow on from it across the lanes. Works in lane coordinates through an
/// <see cref="ILaneLayout"/>, so the same code does rows and columns.
/// </summary>
public static class LaneStacker
{
    /// <summary>Rows (<see cref="LaneLayouts.Horizontal"/>) when the lanes' centres are more spread top to bottom than
    /// left to right, otherwise columns (<see cref="LaneLayouts.Vertical"/>).</summary>
    public static ILaneLayout LayoutOf(List<Group> lanes)
    {
        double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
        foreach (Group g in lanes)
        {
            double cx = g.X + g.W / 2;
            double cy = g.Y + g.H / 2;
            minX = Math.Min(minX, cx);
            maxX = Math.Max(maxX, cx);
            minY = Math.Min(minY, cy);
            maxY = Math.Max(maxY, cy);
        }
        if (maxY - minY >= maxX - minX)
        {
            return LaneLayouts.Horizontal;
        }
        return LaneLayouts.Vertical;
    }

    /// <summary>Where each lane goes, first to last across the lanes. Empty when there are fewer than two lanes.</summary>
    public static List<LanePlace> Plan(List<Group> lanes)
    {
        List<LanePlace> result = new List<LanePlace>();
        if (lanes.Count < 2)
        {
            return result;
        }
        ILaneLayout layout = LayoutOf(lanes);

        // The order they sit in now: by their centre across the lanes, then along the flow.
        List<Group> ordered = new List<Group>(lanes);
        ordered.Sort((a, b) =>
        {
            (double fa, double ca) = layout.ToLane(a.X + a.W / 2, a.Y + a.H / 2);
            (double fb, double cb) = layout.ToLane(b.X + b.W / 2, b.Y + b.H / 2);
            int byCross = ca.CompareTo(cb);
            if (byCross != 0)
            {
                return byCross;
            }
            return fa.CompareTo(fb);
        });

        // The first lane stays; each next lane starts where the one before it ends.
        (double flowStart, double cross) = layout.ToLane(ordered[0].X, ordered[0].Y);
        foreach (Group g in ordered)
        {
            (double x, double y) = layout.FromLane(flowStart, cross);
            result.Add(new LanePlace(g, x, y));
            (double _, double depth) = layout.ToLane(g.W, g.H);
            cross += depth;
        }
        return result;
    }
}
