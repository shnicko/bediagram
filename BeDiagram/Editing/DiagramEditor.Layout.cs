using BeDiagram.Models.Serialization;
using BeDiagram.Editing.Lanes;
using BeDiagram.Editing.Routing;
using BeDiagram.Models;

namespace BeDiagram.Editing;

/// <summary>One-off arrangements: auto-layout, tidy connectors and swim lanes.</summary>
public partial class DiagramEditor
{
    // ───────────── Auto-layout ─────────────

    /// <summary>True when there's anything to lay out.</summary>
    public bool HasShapes => Diagram.Nodes.Count > 0 || Diagram.TextBoxes.Count > 0;

    /// <summary>
    /// Auto-layout: lays the whole diagram out by step number in one direction (shapes and notes in no group, and whole
    /// top-level groups as blocks — see <see cref="LaneArranger.ArrangeAll"/>), then tidies any connector still passing
    /// through a shape (<see cref="ConnectorTidier"/>). One undo step.
    /// </summary>
    public void AutoLayout(ILaneLayout layout)
    {
        if (!HasShapes) { view.ShowToast("Nothing to lay out yet"); return; }
        Change();
        int placed = new LaneArranger(Diagram, layout).ArrangeAll();
        TidyPlan plan = new ConnectorTidier(Diagram, id => FindBox(id)).Plan(Diagram.Edges);
        ConnectorTidier.Apply(plan);
        string tidied = plan.Routes.Count == 0 ? "" : $", {plan.Routes.Count} connector{(plan.Routes.Count == 1 ? "" : "s")} tidied";
        string stuck = plan.Crossing > plan.Cleared ? $" ({plan.Crossing - plan.Cleared} still touch a shape)" : "";
        view.ShowToast($"Laid out {placed} item{(placed == 1 ? "" : "s")} {layout.Direction} by step{tidied}{stuck}");
    }

    // ───────────── Tidy connectors ─────────────

    /// <summary>
    /// Re-routes elbow connectors that pass through a shape or text box (see <see cref="ConnectorTidier"/>): the selected
    /// connector; or the connectors touching the selected items; or, with nothing selected, all of them.
    /// Connectors already clear are left alone. One undo step, and none when nothing changes.
    /// </summary>
    public void TidyConnectors()
    {
        List<Edge> edges;
        string scope;
        if (SelectedEdge is { } one) { edges = new List<Edge> { one }; scope = "the selected connector"; }
        else if (Selection.Count > 0)
        {
            HashSet<string> ids = WithMembers(Selection).Select(b => b.Id).ToHashSet();
            edges = Diagram.Edges.Where(e => ids.Contains(e.From) || ids.Contains(e.To)).ToList();
            scope = "connectors on the selection";
        }
        else { edges = Diagram.Edges.ToList(); scope = "all connectors"; }
        if (edges.Count == 0) { view.ShowToast("No connectors to tidy"); return; }
        TidyPlan plan = new ConnectorTidier(Diagram, id => FindBox(id)).Plan(edges);
        if (plan.Routes.Count > 0) { Change(); ConnectorTidier.Apply(plan); }
        int stuck = plan.Crossing - plan.Cleared;
        if (plan.Routes.Count == 0 && stuck == 0) view.ShowToast($"Nothing to tidy — {scope} already miss every shape");
        else if (stuck == 0) view.ShowToast($"Tidied {plan.Routes.Count} connector{(plan.Routes.Count == 1 ? "" : "s")}");
        else view.ShowToast($"Tidied {plan.Routes.Count} connector{(plan.Routes.Count == 1 ? "" : "s")}; {stuck} still touch a shape — no clear route (move a shape to make room)");
    }

    // ───────────── Swim lanes ─────────────

    /// <summary>The top-level groups (the lanes that <see cref="ArrangeLanes"/> would arrange).</summary>
    public List<Group> TopLevelGroups()
    {
        GroupTree tree = Tree;
        return Diagram.Groups.Where(g => tree.Parent(g) is null).ToList();
    }

    /// <summary>True when there is at least one top-level group to treat as a lane.</summary>
    public bool HasLanes => TopLevelGroups().Count > 0;

    /// <summary>
    /// Arranges top-level groups as swim lanes (see <see cref="LaneArranger"/>): the selected ones when two or more are
    /// selected, otherwise all of them. One undo step.
    /// </summary>
    public void ArrangeLanes(ILaneLayout layout)
    {
        List<Group> top = TopLevelGroups();
        List<Group> picked = top.Where(g => Selection.Contains(g.Id)).ToList();
        List<Group> lanes = picked.Count >= 2 ? picked : top;
        if (lanes.Count == 0) { view.ShowToast("Make a group for each lane first: select its shapes and press Ctrl+G"); return; }
        Change();
        int placed = new LaneArranger(Diagram, layout).Arrange(lanes);
        view.ShowToast($"{lanes.Count} lane{(lanes.Count == 1 ? "" : "s")} arranged ({layout.Name}), {placed} item{(placed == 1 ? "" : "s")} lined up by step");
    }

    /// <summary>
    /// How far the contents would have to move to sit in the middle of the group: the centre of the group minus the
    /// centre of the shrink-wrapped box <see cref="FitGroup"/> would make. Null when the group is empty.
    /// </summary>
    (double Dx, double Dy)? ContentsOffset(Group g)
    {
        List<IBox> members = Members(g).Cast<IBox>().ToList();
        if (members.Count == 0) return null;
        (double x, double y, double w, double h) = Boxes.GroupBounds(members, g);
        return (g.X + g.W / 2 - (x + w / 2), g.Y + g.H / 2 - (y + h / 2));
    }
}
