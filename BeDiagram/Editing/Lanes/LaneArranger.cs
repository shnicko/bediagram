using BeDiagram.Models;

namespace BeDiagram.Editing.Lanes;

/// <summary>
/// Arranges groups as swim lanes, once (a one-off command, like Align). Each lane is a group; what sits directly in it
/// (shapes, text boxes, inner groups) is laid out by step number, so step N lands in the same column in every lane:
/// <list type="number">
/// <item><b>Slots.</b> A shape takes its own step. An inner group moves as one block and takes the earliest step inside
/// it. An outcome, text box or step-less group rides with the step of a shape it's connected to (the one leading into
/// it first) and sits next to that shape. Anything else (an unconnected note) goes in a column after the last step.</item>
/// <item><b>Columns.</b> One column per step used in any lane; shape centres sit on the column's centre line, with
/// room for the widest item (and any label below it).
/// Items sharing a step in a lane (parallel steps, outcomes) stack across the lane, centred.</item>
/// <item><b>Lanes.</b> In their current order (see <see cref="InCurrentOrder"/>), edge to edge, all the same length along the flow, each as deep as
/// its tallest stack. The first lane stays where it starts.</item>
/// </list>
/// Connectors between moved items lose any hand-placed bends and sides so they re-route from the new positions.
/// Works through an <see cref="ILaneLayout"/>, so the same code does left → right and top → down.
/// </summary>
public class LaneArranger
{
    /// <summary>Space between columns along the flow (room for a connector and its label).</summary>
    public const double ColumnGap = 70;
    /// <summary>Space between items stacked in one column of a lane.</summary>
    public const double StackGap = 30;
    /// <summary>Content depth of a lane with nothing in its columns.</summary>
    public const double EmptyLaneDepth = 60;

    readonly Diagram d;
    readonly GroupTree tree;
    readonly ILaneLayout layout;

    public LaneArranger(Diagram diagram, ILaneLayout layout)
    {
        d = diagram;
        tree = new GroupTree(diagram);
        this.layout = layout;
    }

    /// <summary>An item to place: where it goes and how much room it needs (in lane terms).</summary>
    sealed class Placed
    {
        public required IGroupable Item { get; init; }
        public required int Slot { get; init; }
        /// <summary>The shape this item rides with (an outcome's step), or null for an item with its own step.</summary>
        public required string? Anchor { get; init; }
        public required double Flow { get; init; }
        public required double Cross { get; init; }
        /// <summary>Distance along the flow from the footprint's start to the outline's centre.</summary>
        public required double CentreFlow { get; init; }
        /// <summary>Offset of the outline from the footprint's top-left on the canvas (a wide label below a circle).</summary>
        public required double OffsetX { get; init; }
        /// <summary>Where it is now across the lane, to keep parallel steps in their current order.</summary>
        public required double Now { get; init; }
    }

    /// <summary>Arranges <paramref name="lanes"/> (top-level groups). Returns the number of items placed by step.</summary>
    public int Arrange(List<Group> lanes)
    {
        if (lanes.Count == 0) return 0;
        lanes = InCurrentOrder(lanes);

        // 1. Slots for what sits directly in each lane.
        Dictionary<Group, List<Placed>> placed = new Dictionary<Group, List<Placed>>();
        Dictionary<Group, List<IGroupable>> loose = new Dictionary<Group, List<IGroupable>>();
        foreach (Group lane in lanes)
        {
            placed[lane] = new List<Placed>();
            loose[lane] = new List<IGroupable>();
            foreach (IGroupable item in tree.Children(lane).ToList())
            {
                if (SlotOf(item) is { } at) placed[lane].Add(Measure(item, at.Slot, at.Anchor));
                else loose[lane].Add(item);
            }
        }

        // Items with no step to ride with (an unconnected note) get a column of their own after the last step.
        int endSlot = placed.Values.SelectMany(p => p).Select(p => p.Slot).DefaultIfEmpty(0).Max() + 1;
        foreach (Group lane in lanes)
            foreach (IGroupable item in loose[lane]) placed[lane].Add(Measure(item, endSlot, null));

        // 2. Columns: every step used in any lane. Shape centres line up on the column's centre line (so connectors run
        // straight); the column is wide enough for the most room any item needs before and after its centre.
        List<Placed> all = placed.Values.SelectMany(p => p).ToList();
        List<int> slots = all.Select(p => p.Slot).Distinct().OrderBy(s => s).ToList();
        Dictionary<int, double> colBefore = slots.ToDictionary(s => s, s => all.Where(p => p.Slot == s).Max(p => p.CentreFlow));
        Dictionary<int, double> colAfter = slots.ToDictionary(s => s, s => all.Where(p => p.Slot == s).Max(p => p.Flow - p.CentreFlow));
        Dictionary<int, double> colStart = new Dictionary<int, double>();
        double run = 0;
        foreach (int s in slots) { colStart[s] = run; run += colBefore[s] + colAfter[s] + ColumnGap; }
        double contentFlow = slots.Count > 0 ? run - ColumnGap : 0;

        // 3. Lanes: shared start and length along the flow; depth from the tallest stack.
        (double flow0, double cross0) = layout.ToLane(lanes[0].X, lanes[0].Y);
        double maxFlowStart = lanes.Max(g => layout.Insets(g).FlowStart);
        double maxFlowEnd = lanes.Max(g => layout.Insets(g).FlowEnd);
        double laneLength = maxFlowStart + contentFlow + maxFlowEnd;
        double contentFlowStart = flow0 + maxFlowStart;

        HashSet<string> moved = new HashSet<string>();
        double crossAt = cross0;
        foreach (Group lane in lanes)
        {
            (double flowStart, double flowEnd, double crossStart, double crossEnd) = layout.Insets(lane);
            Dictionary<int, List<Placed>> stacks = placed[lane].GroupBy(p => p.Slot).ToDictionary(g => g.Key, g => StackOrder(g.ToList()));
            double depth = stacks.Count == 0 ? EmptyLaneDepth
                : stacks.Values.Max(st => st.Sum(p => p.Cross) + StackGap * (st.Count - 1));

            // The lane box.
            (double newX, double newY) = layout.FromLane(flow0, crossAt);
            (double w, double h) = layout.FromLane(laneLength, crossStart + depth + crossEnd);
            lane.X = newX; lane.Y = newY; lane.W = w; lane.H = h;
            moved.Add(lane.Id);

            double contentCross = crossAt + crossStart;
            foreach ((int slot, List<Placed> stack) in stacks)
            {
                double stackDepth = stack.Sum(p => p.Cross) + StackGap * (stack.Count - 1);
                double c = contentCross + (depth - stackDepth) / 2;
                foreach (Placed p in stack)
                {
                    double f = contentFlowStart + colStart[slot] + colBefore[slot] - p.CentreFlow;
                    (double fx, double fy) = layout.FromLane(f, c);
                    MoveTo(p.Item, Math.Round(fx + p.OffsetX), Math.Round(fy), moved);
                    c += p.Cross + StackGap;
                }
            }
            crossAt += crossStart + depth + crossEnd;
        }

        // 4. Connectors between moved items re-route from scratch.
        foreach (Edge e in d.Edges.Where(e => moved.Contains(e.From) && moved.Contains(e.To))) e.ResetRoute();
        return placed.Values.Sum(p => p.Count);
    }

    /// <summary>
    /// Order within one column of a lane: items with their own step in their current order across the lane, each
    /// followed straight away by what rides with it (an outcome next to its decision), then anything else.
    /// </summary>
    static List<Placed> StackOrder(List<Placed> stack)
    {
        List<Placed> order = stack.Where(p => p.Anchor is null).OrderBy(p => p.Now).ToList();
        foreach (Placed rider in stack.Where(p => p.Anchor is not null).OrderBy(p => p.Now))
        {
            int at = order.FindLastIndex(p => p.Item.Id == rider.Anchor || p.Anchor == rider.Anchor);
            if (at >= 0) order.Insert(at + 1, rider);
            else order.Add(rider);
        }
        return order;
    }

    /// <summary>
    /// The lanes in the order they sit now: along whichever axis their centres are more spread out (rows stacked top to
    /// bottom stay in that order when they become columns left to right, and the other way round).
    /// </summary>
    static List<Group> InCurrentOrder(List<Group> lanes)
    {
        double spreadX = lanes.Max(g => g.X + g.W / 2) - lanes.Min(g => g.X + g.W / 2);
        double spreadY = lanes.Max(g => g.Y + g.H / 2) - lanes.Min(g => g.Y + g.H / 2);
        return spreadY >= spreadX
            ? lanes.OrderBy(g => g.Y + g.H / 2).ThenBy(g => g.X).ToList()
            : lanes.OrderBy(g => g.X + g.W / 2).ThenBy(g => g.Y).ToList();
    }

    /// <summary>The step an item lines up with, and the shape it rides with when that isn't its own step
    /// (null Anchor = its own step); null when it has none.</summary>
    (int Slot, string? Anchor)? SlotOf(IGroupable item)
    {
        switch (item)
        {
            case Node n when StepOrder.IsStep(n) && n.Step is int s:
                return (s, null);
            case Group g:
                List<int> inside = tree.Descendants(g).OfType<Node>().Where(n => StepOrder.IsStep(n) && n.Step is not null)
                    .Select(n => n.Step!.Value).ToList();
                if (inside.Count > 0) return (inside.Min(), null);
                break;
        }
        // Outcomes, text boxes and step-less groups ride with a connected step: what leads into them first.
        Dictionary<string, Node> steps = d.Nodes.Where(n => StepOrder.IsStep(n) && n.Step is not null).ToDictionary(n => n.Id);
        Node? from = d.Edges.Where(e => e.To == item.Id && steps.ContainsKey(e.From)).Select(e => steps[e.From])
            .OrderByDescending(n => n.Step).FirstOrDefault()
            ?? d.Edges.Where(e => e.From == item.Id && steps.ContainsKey(e.To)).Select(e => steps[e.To])
            .OrderByDescending(n => n.Step).FirstOrDefault();
        return from is null ? null : (from.Step!.Value, from.Id);
    }

    /// <summary>The room an item needs: its outline, plus a label or description drawn below or wider than it.</summary>
    Placed Measure(IGroupable item, int slot, string? anchor)
    {
        double left = item.X, right = item.X + item.W;
        if (item is Node n && Geometry.OutsideTextBox(n, Geometry.Layout(n)) is { } r)
        {
            left = Math.Min(left, r.X);
            right = Math.Max(right, r.X + r.W);
        }
        double w = right - left, h = item.H + Geometry.ExtraBelow(item);
        (double flow, double cross) = layout.ToLane(w, h);
        double centreFlow = layout.ToLane(item.X - left + item.W / 2, item.H / 2).Flow;
        return new Placed
        {
            Item = item, Slot = slot, Anchor = anchor, Flow = flow, Cross = cross, CentreFlow = centreFlow,
            OffsetX = item.X - left, Now = layout.ToLane(item.X, item.Y).Cross,
        };
    }

    /// <summary>Moves an item (a group carries everything inside it) and records what moved.</summary>
    void MoveTo(IGroupable item, double x, double y, HashSet<string> moved)
    {
        double dx = x - item.X, dy = y - item.Y;
        item.X = x; item.Y = y;
        moved.Add(item.Id);
        if (item is Group g)
            foreach (IGroupable m in tree.Descendants(g)) { m.X += dx; m.Y += dy; moved.Add(m.Id); }
    }
}
