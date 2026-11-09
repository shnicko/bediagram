using BeDiagram.Models;

namespace BeDiagram.Editing;

/// <summary>Step order (Node.Step): renumbering, moving, running in parallel; see StepOrder.</summary>
public partial class DiagramEditor
{
    // ───────────── Step order ─────────────

    /// <summary>Show or hide the step-number badges. The order itself is kept either way.</summary>
    public void SetShowSteps(bool show) { if (Diagram.ShowSteps == show) return; Change(); Diagram.ShowSteps = show; }

    /// <summary>Makes the shape step <paramref name="step"/> on its own; the steps from there on move down one.</summary>
    public void SetStep(Node n, int? step)
    {
        if (step is not int s || s == n.Step && !Steps.IsParallel(n)) return;
        Change();
        Steps.MoveTo(n, s);
    }

    /// <summary>Runs the shape in parallel with the step before it (shares that number), or back on its own.</summary>
    public void SetParallel(Node n, bool parallel) { if (parallel == Steps.IsParallel(n)) return; Change(); Steps.SetParallel(n, parallel); }

    /// <summary>Step order dialog: makes the shape a step of its own in gap <paramref name="gap"/> (just before step
    /// <paramref name="gap"/>; see <see cref="StepOrder.MoveBefore"/>).</summary>
    public void MoveStepBefore(Node n, int gap) { if (!Steps.CanMoveBefore(n, gap)) return; Change(); Steps.MoveBefore(n, gap); }

    /// <summary>Step order dialog: one place earlier (a parallel step comes out just before its group).</summary>
    public void MoveStepEarlier(Node n) => MoveStepBefore(n, Steps.EarlierGap(n));

    /// <summary>Step order dialog: one place later (a parallel step comes out just after its group).</summary>
    public void MoveStepLater(Node n) => MoveStepBefore(n, Steps.LaterGap(n));

    /// <summary>Step order dialog: the shape runs in parallel with step <paramref name="step"/>.</summary>
    public void JoinStep(Node n, int step) { if (!Steps.CanJoin(n, step)) return; Change(); Steps.Join(n, step); }

    /// <summary>The selected shapes all take the earliest of their step numbers, so they run in parallel.</summary>
    public void MakeSelectionParallel()
    {
        List<Node> nodes = Diagram.Nodes.Where(n => Selection.Contains(n.Id) && n.Kind.IsStep).ToList();
        if (nodes.Count < 2) { view.ShowToast("Select two or more steps to run in parallel"); return; }
        Change();
        Steps.MakeParallel(nodes);
        view.ShowToast($"{nodes.Count} steps now share step {nodes[0].Step}");
    }

    /// <summary>The two selected shapes, when exactly two shapes (and nothing else) are selected; otherwise null.</summary>
    public (Node A, Node B)? SwapPair()
    {
        if (Selection.Count != 2) return null;
        List<Node> nodes = Diagram.Nodes.Where(n => Selection.Contains(n.Id)).ToList();
        return nodes.Count == 2 ? (nodes[0], nodes[1]) : null;
    }

    /// <summary><paramref name="shape"/> moved into <paramref name="place"/>: a copy that keeps the place's id (so its
    /// connectors stay), group, centre and, if asked, step number.</summary>
    static Node TakePlace(Node shape, Node place, bool keepStep)
    {
        Node n = shape.Copy();
        n.Id = place.Id;
        n.GroupId = place.GroupId;
        n.X = place.X + place.W / 2 - n.W / 2;
        n.Y = place.Y + place.H / 2 - n.H / 2;
        if (keepStep) n.Step = place.Step;
        return n;
    }

    /// <summary>The selected shapes become separate steps in turn (by position), starting where the first of them is.</summary>
    public void MakeSelectionSequential()
    {
        List<Node> nodes = Diagram.Nodes.Where(n => Selection.Contains(n.Id) && n.Kind.IsStep && n.Step is not null)
            .OrderBy(n => n.Step).ThenBy(n => n.Y).ThenBy(n => n.X).ToList();
        if (nodes.Count < 2) { view.ShowToast("Select two or more steps"); return; }
        Change();
        int start = nodes[0].Step!.Value;
        for (int i = 0; i < nodes.Count; i++) Steps.MoveTo(nodes[i], start + i);
        view.ShowToast($"Steps {start}–{start + nodes.Count - 1} in turn");
    }
}
