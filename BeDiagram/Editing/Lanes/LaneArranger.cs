using BeDiagram.Models;
using BeDiagram.Models.Text;

namespace BeDiagram.Editing.Lanes;

/// <summary>
/// Arranges groups as swim lanes (<see cref="Arrange"/>), or the whole diagram as one lane with no box (auto-layout,
/// <see cref="ArrangeAll"/>), once (a one-off command, like Align). Each lane is a group; what sits directly in it
/// (shapes, text boxes, inner groups) is laid out by step number, so step N lands in the same column in every lane:
/// <list type="number">
/// <item><b>Slots.</b> A shape takes its own step. An inner group moves as one block and takes the earliest step inside
/// it. An outcome, text box or step-less group rides with the step of a shape it's connected to (the one leading into
/// it first) and sits next to that shape. Anything else (an unconnected note) goes in a column after the last step.</item>
/// <item><b>Columns.</b> One column per step used in any lane; shape centres sit on the column's centre line, with
/// room for the widest item (and any label below it).
/// Items sharing a step in a lane (parallel steps, outcomes) stack across the lane, centred; a decision's outcomes go
/// one each side of it, with room for the Yes / No labels; steps sharing a column follow the steps that lead into them,
/// so branches don't cross.</item>
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
    /// <summary>Extra space between stacked neighbours joined by a labelled connector, so the label isn't squeezed.</summary>
    public const double LabelRoom = 26;
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

    /// <summary>A lane to lay out: its box (null for auto-layout, which has no box), what sits in it, and the room
    /// its title takes.</summary>
    sealed class Lane
    {
        public required Group? Box { get; init; }
        public required List<IGroupable> Items { get; init; }
        public required (double FlowStart, double FlowEnd, double CrossStart, double CrossEnd) Insets { get; init; }
    }

    /// <summary>Arranges <paramref name="lanes"/> (top-level groups) as swim lanes. Returns the number of items placed.</summary>
    public int Arrange(List<Group> lanes)
    {
        if (lanes.Count == 0) return 0;
        lanes = InCurrentOrder(lanes);
        List<Lane> list = lanes.Select(g => new Lane { Box = g, Items = tree.Children(g).ToList(), Insets = layout.Insets(g) }).ToList();
        (double flow0, double cross0) = layout.ToLane(lanes[0].X, lanes[0].Y);
        return Place(list, flow0, cross0);
    }

    /// <summary>
    /// Auto-layout: everything at the top level (shapes and text boxes in no group, and whole top-level groups as
    /// blocks) laid out by step as one lane with no box, starting at the drawing's current top-left corner.
    /// Returns the number of items placed.
    /// </summary>
    public int ArrangeAll()
    {
        List<IGroupable> items = new List<IGroupable>();
        items.AddRange(d.Nodes.Where(n => tree.Parent(n) is null));
        items.AddRange(d.TextBoxes.Where(t => tree.Parent(t) is null));
        items.AddRange(d.Groups.Where(g => tree.Parent(g) is null));
        if (items.Count == 0) return 0;
        (double flow0, double cross0) = layout.ToLane(items.Min(b => b.X), items.Min(b => b.Y));
        Lane all = new Lane { Box = null, Items = items, Insets = (0, 0, 0, 0) };
        return Place(new List<Lane> { all }, flow0, cross0);
    }

    /// <summary>The layout itself, shared by swim lanes and auto-layout (see the class summary for the rules).</summary>
    int Place(List<Lane> lanes, double flow0, double cross0)
    {
        // 1. Slots for what sits directly in each lane.
        Dictionary<Lane, List<Placed>> placed = new Dictionary<Lane, List<Placed>>();
        Dictionary<Lane, List<IGroupable>> loose = new Dictionary<Lane, List<IGroupable>>();
        foreach (Lane lane in lanes)
        {
            placed[lane] = new List<Placed>();
            loose[lane] = new List<IGroupable>();
            foreach (IGroupable item in lane.Items)
            {
                if (SlotOf(item) is { } at) placed[lane].Add(Measure(item, at.Slot, at.Anchor));
                else loose[lane].Add(item);
            }
        }

        // Items with no step to ride with (an unconnected note) get a column of their own after the last step.
        int endSlot = placed.Values.SelectMany(p => p).Select(p => p.Slot).DefaultIfEmpty(0).Max() + 1;
        foreach (Lane lane in lanes)
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
        double maxFlowStart = lanes.Max(l => l.Insets.FlowStart);
        double maxFlowEnd = lanes.Max(l => l.Insets.FlowEnd);
        double laneLength = maxFlowStart + contentFlow + maxFlowEnd;
        double contentFlowStart = flow0 + maxFlowStart;

        // Order inside each stack, column by column from the start of the flow: steps follow the steps that lead into
        // them (the average position of their predecessors, already placed in earlier columns), so branches don't cross.
        Dictionary<Lane, Dictionary<int, List<Placed>>> orders = lanes.ToDictionary(l => l, _ => new Dictionary<int, List<Placed>>());
        Dictionary<string, double> across = new Dictionary<string, double>();
        foreach (int slot in slots)
            for (int li = 0; li < lanes.Count; li++)
            {
                List<Placed> items = placed[lanes[li]].Where(p => p.Slot == slot).ToList();
                if (items.Count == 0) continue;
                List<Placed> stack = StackOrder(items, p => Barycentre(p, across));
                orders[lanes[li]][slot] = stack;
                // Where each item sits across its lane, relative to the stack's centre (stacks are centred), with the
                // lane's place in the order so items in different lanes compare the right way round.
                double c = -DepthOf(stack) / 2;
                for (int i = 0; i < stack.Count; i++)
                {
                    double centre = li * 1e6 + c + stack[i].Cross / 2;
                    foreach (string id in IdsOf(stack[i].Item)) across[id] = centre;
                    if (i + 1 < stack.Count) c += stack[i].Cross + GapBetween(stack[i], stack[i + 1]);
                }
            }

        HashSet<string> moved = new HashSet<string>();
        double crossAt = cross0;
        foreach (Lane lane in lanes)
        {
            (double flowStart, double flowEnd, double crossStart, double crossEnd) = lane.Insets;
            Dictionary<int, List<Placed>> stacks = orders[lane];
            double depth = stacks.Count == 0 ? EmptyLaneDepth
                : stacks.Values.Max(DepthOf);

            // The lane box (auto-layout has none).
            if (lane.Box is Group box)
            {
                (double newX, double newY) = layout.FromLane(flow0, crossAt);
                (double w, double h) = layout.FromLane(laneLength, crossStart + depth + crossEnd);
                box.X = newX; box.Y = newY; box.W = w; box.H = h;
                moved.Add(box.Id);
            }

            double contentCross = crossAt + crossStart;
            foreach ((int slot, List<Placed> stack) in stacks)
            {
                double stackDepth = DepthOf(stack);
                double c = contentCross + (depth - stackDepth) / 2;
                for (int i = 0; i < stack.Count; i++)
                {
                    Placed p = stack[i];
                    double f = contentFlowStart + colStart[slot] + colBefore[slot] - p.CentreFlow;
                    (double fx, double fy) = layout.FromLane(f, c);
                    MoveTo(p.Item, Math.Round(fx + p.OffsetX), Math.Round(fy), moved);
                    if (i + 1 < stack.Count) c += p.Cross + GapBetween(p, stack[i + 1]);
                }
            }
            crossAt += crossStart + depth + crossEnd;
        }

        // 4. Connectors between moved items re-route from scratch.
        foreach (Edge e in d.Edges.Where(e => moved.Contains(e.From) && moved.Contains(e.To))) e.ResetRoute();
        return placed.Values.Sum(p => p.Count);
    }

    /// <summary>
    /// Order within one column of a lane: items with their own step follow what leads into them (see
    /// <see cref="Barycentre"/>; the first column keeps its current order across the lane), with what
    /// rides with each one placed right beside it — on alternate sides, so a decision's two outcomes go one each side
    /// (Yes one way, No the other) and neither branch has to loop round the other. Anything else goes at the end.
    /// </summary>
    static List<Placed> StackOrder(List<Placed> stack, Func<Placed, double?> barycentre)
    {
        // Steps with something leading into them follow it; the rest (the start of the flow) keep their current order.
        List<Placed> steps = stack.Where(p => p.Anchor is null).ToList();
        List<Placed> order = steps.Where(p => barycentre(p) is not null).OrderBy(p => barycentre(p)).ThenBy(p => p.Now)
            .Concat(steps.Where(p => barycentre(p) is null).OrderBy(p => p.Now)).ToList();
        Dictionary<string, int> ridersSoFar = new Dictionary<string, int>();
        foreach (Placed rider in stack.Where(p => p.Anchor is not null).OrderBy(p => p.Now))
        {
            int anchorAt = order.FindIndex(p => p.Item.Id == rider.Anchor);
            if (anchorAt < 0) { order.Add(rider); continue; }
            int n = ridersSoFar.TryGetValue(rider.Anchor!, out int k) ? k : 0;
            ridersSoFar[rider.Anchor!] = n + 1;
            if (n % 2 == 0)
            {
                // After the anchor, beyond any earlier riders already on that side.
                int at = anchorAt + 1;
                while (at < order.Count && order[at].Anchor == rider.Anchor) at++;
                order.Insert(at, rider);
            }
            else
            {
                // Before the anchor, beyond any earlier riders already on that side.
                int at = anchorAt;
                while (at > 0 && order[at - 1].Anchor == rider.Anchor) at--;
                order.Insert(at, rider);
            }
        }
        return order;
    }

    /// <summary>The average position across the lanes of what leads into <paramref name="p"/> (connectors into it, or
    /// into anything inside it when it's a group), from items already placed; null when nothing placed leads into it.</summary>
    double? Barycentre(Placed p, Dictionary<string, double> across)
    {
        HashSet<string> ids = IdsOf(p.Item).ToHashSet();
        List<double> from = d.Edges.Where(e => ids.Contains(e.To) && !ids.Contains(e.From) && across.ContainsKey(e.From))
            .Select(e => across[e.From]).ToList();
        return from.Count > 0 ? from.Average() : null;
    }

    /// <summary>The item's id, plus everything inside it when it's a group.</summary>
    IEnumerable<string> IdsOf(IGroupable item)
    {
        yield return item.Id;
        if (item is Group g)
            foreach (IGroupable m in tree.Descendants(g)) yield return m.Id;
    }

    /// <summary>Space between two neighbours in a stack: <see cref="StackGap"/>, plus room for a label when a labelled
    /// connector joins them (a decision's "Yes" next to its outcome).</summary>
    double GapBetween(Placed x, Placed y) =>
        StackGap + (d.Edges.Any(e => !string.IsNullOrWhiteSpace(e.Label)
                                    && (e.From == x.Item.Id && e.To == y.Item.Id || e.From == y.Item.Id && e.To == x.Item.Id)) ? LabelRoom : 0);

    /// <summary>How deep a stack is across the lane, items plus the gaps between them.</summary>
    double DepthOf(List<Placed> stack)
    {
        double depth = stack.Sum(p => p.Cross);
        for (int i = 0; i + 1 < stack.Count; i++) depth += GapBetween(stack[i], stack[i + 1]);
        return depth;
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
            case Node n when n.Kind.IsStep && n.Step is int s:
                return (s, null);
            case Group g:
                List<int> inside = tree.Descendants(g).OfType<Node>().Where(n => n.Kind.IsStep && n.Step is not null)
                    .Select(n => n.Step!.Value).ToList();
                if (inside.Count > 0) return (inside.Min(), null);
                break;
        }
        // Outcomes, text boxes and step-less groups ride with a connected step: what leads into them first.
        Dictionary<string, Node> steps = d.Nodes.Where(n => n.Kind.IsStep && n.Step is not null).ToDictionary(n => n.Id);
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
        if (item is Node n && TextLayout.OutsideTextBox(n, TextLayout.Layout(n)) is { } r)
        {
            left = Math.Min(left, r.X);
            right = Math.Max(right, r.X + r.W);
        }
        double w = right - left, h = item.H + TextLayout.ExtraBelow(item);
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
