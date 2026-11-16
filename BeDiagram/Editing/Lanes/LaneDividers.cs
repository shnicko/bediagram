using BeDiagram.Models;

namespace BeDiagram.Editing.Lanes;

/// <summary>
/// Finds the edges shared by touching lanes (top-level groups), so dragging one resizes both lanes at once: the lane
/// before the edge gets longer as the one after it gets shorter, and nothing inside either lane moves.
/// </summary>
public static class LaneDividers
{
    /// <summary>How close (px) two edges must be to count as touching.</summary>
    public const double Touching = 0.5;

    /// <summary>The draggable part of an edge stops this far short of each corner (the corner grips stay reachable).</summary>
    public const double CornerRoom = 12;

    /// <summary>Half the width of the gap left around each ● port on the edge (ports are 6 px circles).</summary>
    public const double PortRoom = 9;

    /// <summary>Every edge shared by two of <paramref name="lanes"/>, with the stretch along it that can be dragged.</summary>
    public static List<LaneDivider> Find(List<Group> lanes)
    {
        List<LaneDivider> result = new List<LaneDivider>();
        foreach (Group a in lanes)
        {
            foreach (Group b in lanes)
            {
                if (a == b)
                {
                    continue;
                }
                // a above b: a's bottom edge is b's top edge.
                if (Math.Abs(a.Y + a.H - b.Y) <= Touching)
                {
                    LaneDivider? d = Make(a, b, true, b.Y, a.X, a.X + a.W, b.X, b.X + b.W);
                    if (d is not null)
                    {
                        result.Add(d);
                    }
                }
                // a left of b: a's right edge is b's left edge.
                if (Math.Abs(a.X + a.W - b.X) <= Touching)
                {
                    LaneDivider? d = Make(a, b, false, b.X, a.Y, a.Y + a.H, b.Y, b.Y + b.H);
                    if (d is not null)
                    {
                        result.Add(d);
                    }
                }
            }
        }
        return result;
    }

    /// <summary>
    /// The divider between <paramref name="before"/> and <paramref name="after"/>, given each lane's extent along the
    /// edge; null when they share too little of it to drag.
    /// </summary>
    static LaneDivider? Make(Group before, Group after, bool rows, double at, double beforeFrom, double beforeTo,
                             double afterFrom, double afterTo)
    {
        double from = Math.Max(beforeFrom, afterFrom) + CornerRoom;
        double to = Math.Min(beforeTo, afterTo) - CornerRoom;
        if (to - from < 2 * PortRoom)
        {
            return null;
        }

        // Each lane has a ● port in the middle of this edge; leave a gap around both so connecting still works.
        List<double> ports = new List<double>();
        ports.Add((beforeFrom + beforeTo) / 2);
        ports.Add((afterFrom + afterTo) / 2);
        ports.Sort();

        List<(double From, double To)> spans = new List<(double From, double To)>();
        double start = from;
        foreach (double p in ports)
        {
            double gapFrom = p - PortRoom;
            double gapTo = p + PortRoom;
            if (gapTo <= start || gapFrom >= to)
            {
                continue;
            }
            if (gapFrom > start)
            {
                spans.Add((start, gapFrom));
            }
            start = Math.Max(start, gapTo);
        }
        if (to > start)
        {
            spans.Add((start, to));
        }
        if (spans.Count == 0)
        {
            return null;
        }
        return new LaneDivider(before, after, rows, at, spans);
    }
}
