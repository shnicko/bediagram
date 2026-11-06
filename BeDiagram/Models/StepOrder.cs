using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models;

/// <summary>
/// The order of the steps in a diagram. Every shape except outcomes carries a <see cref="Node.Step"/> number, always,
/// whether or not the numbers are shown (<see cref="Diagram.ShowSteps"/>). Steps that run in parallel share a number.
/// Numbers are kept gap-free: 1, 2, 3, … with no holes, so "step 4" always means the fourth stage of the flow.
/// Like <see cref="GroupTree"/>, this works on the diagram it's given and holds no state of its own, so it never goes
/// out of date. A later reorder dialog drives the same methods (<see cref="MoveTo"/>, <see cref="SetParallel"/>).
/// </summary>
public class StepOrder
{
    readonly Diagram d;

    public StepOrder(Diagram diagram)
    {
        d = diagram;
    }

    /// <summary>Outcomes are results of a step, not steps, so they get no number.</summary>
    public static bool IsStep(Node n) => n.Type != NodeTypes.Outcome;

    /// <summary>The numbered shapes in step order (parallel steps by position: top to bottom, then left to right).</summary>
    public List<Node> Ordered() =>
        d.Nodes.Where(n => IsStep(n) && n.Step is not null).OrderBy(n => n.Step).ThenBy(n => n.Y).ThenBy(n => n.X).ToList();

    /// <summary>The highest step number in use (0 when there are none).</summary>
    public int Last => d.Nodes.Where(IsStep).Select(n => n.Step ?? 0).DefaultIfEmpty(0).Max();

    /// <summary>The number a new step gets: one after the last.</summary>
    public int Next => Last + 1;

    /// <summary>Other shapes sharing <paramref name="n"/>'s number (its parallel steps).</summary>
    public List<Node> ParallelWith(Node n) =>
        n.Step is null ? new List<Node>() : d.Nodes.Where(o => !ReferenceEquals(o, n) && IsStep(o) && o.Step == n.Step).ToList();

    /// <summary>True when <paramref name="n"/> shares its number with another shape.</summary>
    public bool IsParallel(Node n) => ParallelWith(n).Count > 0;

    /// <summary>
    /// Puts every shape in order: outcomes lose their number, shapes without one are numbered after the last step
    /// (in flow order — following connectors from where the flow starts, then by position), and the numbers are closed
    /// up to 1, 2, 3, … keeping parallel steps together. Safe to call any time; it changes nothing on a tidy diagram.
    /// </summary>
    public void Normalise()
    {
        foreach (Node n in d.Nodes.Where(n => !IsStep(n))) n.Step = null;
        int next = Next;
        foreach (Node n in FlowOrder(d.Nodes.Where(n => IsStep(n) && n.Step is null).ToList()))
            n.Step = next++;
        Compact();
    }

    /// <summary>Closes gaps: the distinct numbers in use become 1, 2, 3, … in the same order.</summary>
    public void Compact()
    {
        List<int> used = d.Nodes.Where(IsStep).Where(n => n.Step is not null).Select(n => n.Step!.Value).Distinct().OrderBy(s => s).ToList();
        Dictionary<int, int> map = new Dictionary<int, int>();
        for (int i = 0; i < used.Count; i++) map[used[i]] = i + 1;
        foreach (Node n in d.Nodes.Where(n => IsStep(n) && n.Step is not null)) n.Step = map[n.Step!.Value];
    }

    /// <summary>
    /// Makes <paramref name="n"/> step <paramref name="step"/> on its own: the shapes from that number on move down one,
    /// and its old place closes up. (To share a number instead, use <see cref="SetParallel"/> or <see cref="MakeParallel"/>.)
    /// </summary>
    public void MoveTo(Node n, int step)
    {
        if (!IsStep(n)) return;
        bool wasAlone = !IsParallel(n);
        int? old = n.Step;
        n.Step = null;
        // Leaving a number of its own closes that gap first, so "move to 5" means the 5th place in the final order.
        if (wasAlone && old is not null)
            foreach (Node o in d.Nodes.Where(o => IsStep(o) && o.Step > old)) o.Step--;
        step = Math.Clamp(step, 1, Next);
        foreach (Node o in d.Nodes.Where(o => IsStep(o) && o.Step >= step)) o.Step++;
        n.Step = step;
        Compact();
    }

    /// <summary>
    /// Parallel on: <paramref name="n"/> joins the step before it (or the one after, when it is step 1).
    /// Parallel off: it leaves its shared number and becomes its own step straight after it.
    /// </summary>
    public void SetParallel(Node n, bool parallel)
    {
        if (!IsStep(n) || n.Step is not int s || parallel == IsParallel(n)) return;
        if (parallel)
        {
            if (Last < 2) return;
            n.Step = s > 1 ? s - 1 : 2;
            Compact();
        }
        else
        {
            foreach (Node o in d.Nodes.Where(o => IsStep(o) && o.Step > s)) o.Step++;
            n.Step = s + 1;
        }
    }

    // ───── Reorder dialog ─────
    // The dialog shows the steps as numbered slots with gaps between them. Gap k sits just before slot k
    // (gap 1 = before the first step, gap Next = after the last). Dropping a shape in a gap makes it a step of its own
    // there; dropping it on a slot makes it run in parallel with that step.

    /// <summary>True when putting <paramref name="n"/> in gap <paramref name="gap"/> would change anything.
    /// A shape that is a step of its own doesn't move when dropped in the gap just before or just after it.</summary>
    public bool CanMoveBefore(Node n, int gap)
    {
        if (!IsStep(n) || n.Step is not int s || gap < 1 || gap > Next) return false;
        return IsParallel(n) || (gap != s && gap != s + 1);
    }

    /// <summary>
    /// Makes <paramref name="n"/> a step of its own in gap <paramref name="gap"/> (see above): it leaves any parallel
    /// step it was part of, the steps from the gap on move down one, and its old place closes up.
    /// </summary>
    public void MoveBefore(Node n, int gap)
    {
        if (!CanMoveBefore(n, gap)) return;
        int s = n.Step!.Value;
        // A step of its own leaves a hole when it goes, so the slots after it are one lower by the time it lands.
        MoveTo(n, !IsParallel(n) && s < gap ? gap - 1 : gap);
    }

    /// <summary>The gap that moves <paramref name="n"/> one place earlier: a step of its own swaps with the step before;
    /// a parallel step comes out of its group and goes just before it.</summary>
    public int EarlierGap(Node n) => n.Step is int s ? (IsParallel(n) ? s : s - 1) : 0;

    /// <summary>The gap that moves <paramref name="n"/> one place later: a step of its own swaps with the step after;
    /// a parallel step comes out of its group and goes just after it.</summary>
    public int LaterGap(Node n) => n.Step is int s ? (IsParallel(n) ? s + 1 : s + 2) : 0;

    /// <summary>True when <paramref name="n"/> could join step <paramref name="step"/> (a different, existing step).</summary>
    public bool CanJoin(Node n, int step) => IsStep(n) && n.Step is int s && s != step && step >= 1 && step <= Last;

    /// <summary><paramref name="n"/> takes number <paramref name="step"/> so it runs in parallel with that step; if it was
    /// a step of its own, the gap it leaves closes up.</summary>
    public void Join(Node n, int step)
    {
        if (!CanJoin(n, step)) return;
        n.Step = step;
        Compact();
    }

    /// <summary>The shapes all take the earliest of their numbers, so they run in parallel; the gaps close up.</summary>
    public void MakeParallel(IEnumerable<Node> nodes)
    {
        List<Node> steps = nodes.Where(n => IsStep(n) && n.Step is not null).ToList();
        if (steps.Count < 2) return;
        int first = steps.Min(n => n.Step!.Value);
        foreach (Node n in steps) n.Step = first;
        Compact();
    }

    /// <summary>
    /// Flow order of <paramref name="nodes"/>, used to number shapes that have no number yet (older files):
    /// <list type="number">
    /// <item>Loops are cut where they turn back: walking the connectors from the shapes in reading order (top to bottom,
    /// then left to right), a connector leading back to a shape still on the current path is a loop-back and is ignored.</item>
    /// <item>The rest is numbered so every shape comes after the shapes leading into it; when several are free to go
    /// next, the one first in reading order goes first.</item>
    /// </list>
    /// So a loop like "Model updates → back to Exchange models" doesn't put the start of the flow at the end.
    /// </summary>
    List<Node> FlowOrder(List<Node> nodes)
    {
        if (nodes.Count == 0) return nodes;
        List<Node> reading = nodes.OrderBy(n => n.Y).ThenBy(n => n.X).ToList();
        Dictionary<string, int> rank = new Dictionary<string, int>();
        for (int i = 0; i < reading.Count; i++) rank[reading[i].Id] = i;
        List<Edge> links = d.Edges.Where(e => rank.ContainsKey(e.From) && rank.ContainsKey(e.To) && e.From != e.To).ToList();
        Dictionary<string, List<string>> outs = reading.ToDictionary(n => n.Id, n =>
            links.Where(e => e.From == n.Id).Select(e => e.To).Distinct().OrderBy(id => rank[id]).ToList());

        // 1. Find the loop-backs (depth-first, without recursion so a long chain can't overflow the stack).
        HashSet<(string From, string To)> loopBacks = new HashSet<(string From, string To)>();
        Dictionary<string, int> state = new Dictionary<string, int>(); // missing = not seen, 1 = on the path, 2 = done
        foreach (Node root in reading)
        {
            if (state.ContainsKey(root.Id)) continue;
            Stack<(string Id, int Next)> path = new Stack<(string Id, int Next)>();
            path.Push((root.Id, 0)); state[root.Id] = 1;
            while (path.Count > 0)
            {
                (string id, int next) = path.Pop();
                if (next < outs[id].Count)
                {
                    path.Push((id, next + 1));
                    string to = outs[id][next];
                    if (!state.TryGetValue(to, out int st)) { state[to] = 1; path.Push((to, 0)); }
                    else if (st == 1) loopBacks.Add((id, to));
                }
                else state[id] = 2;
            }
        }

        // 2. Each shape after those leading into it; reading order breaks ties.
        Dictionary<string, int> waitingOn = reading.ToDictionary(n => n.Id, _ => 0);
        foreach ((string from, List<string> tos) in outs)
            foreach (string to in tos.Where(to => !loopBacks.Contains((from, to)))) waitingOn[to]++;
        SortedSet<int> ready = new SortedSet<int>(reading.Where(n => waitingOn[n.Id] == 0).Select(n => rank[n.Id]));
        List<Node> order = new List<Node>();
        while (ready.Count > 0)
        {
            int r = ready.Min; ready.Remove(r);
            Node cur = reading[r];
            order.Add(cur);
            foreach (string to in outs[cur.Id].Where(to => !loopBacks.Contains((cur.Id, to))))
                if (--waitingOn[to] == 0) ready.Add(rank[to]);
        }
        return order;
    }
}
