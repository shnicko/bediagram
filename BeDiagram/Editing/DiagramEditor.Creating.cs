using BeDiagram.Editing.Lanes;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Text;

namespace BeDiagram.Editing;

/// <summary>Adding shapes, text boxes and groups: from the palette, quick-add, quick branch, joining and new connectors.</summary>
public partial class DiagramEditor
{
    // ───────────── Creating items ─────────────

    /// <summary>Adds a connector (false when it isn't allowed or already exists). <paramref name="fromItem"/> /
    /// <paramref name="toItem"/> attach an end to a row of a list or IPO node instead of the whole node.</summary>
    public bool AddEdge(string from, string to, string label = "", string? fromItem = null, string? toItem = null)
    {
        if (from == to) return false;
        if (Diagram.Edges.Any(e => e.From == from && e.To == to && e.FromItem == fromItem && e.ToItem == toItem)) return false;
        IBox? a = FindBox(from); IBox? b = FindBox(to);
        if (a is null || b is null) return false;
        // Two comments pinned to each other is meaningless; any other pairing (text ↔ shape, text ↔ text) is fine.
        bool aNote = a is TextBox { IsComment: true }, bNote = b is TextBox { IsComment: true };
        if (aNote && bNote) return false;
        // A group connecting to something inside it (at any depth) is meaningless.
        GroupTree tree = Tree;
        if (a is Group ga && b is IGroupable ib && tree.IsInside(ib, ga)) return false;
        if (b is Group gb && a is IGroupable ia && tree.IsInside(ia, gb)) return false;
        Edge edge = new Edge { From = from, To = to, Label = label, FromItem = fromItem, ToItem = toItem };
        edge.CopyLookFrom(Diagram.DefaultLook);
        edge.Style = Diagram.DefaultConnector;
        if (aNote || bNote) TextBox.ApplyPinLook(edge);
        Diagram.Edges.Add(edge);
        return true;
    }

    /// <summary>The item new shapes are auto-joined to: the most recently selected shape, group or (non-comment) text box.</summary>
    IBox? JoinSource()
    {
        if (Selection.Count == 0) return null;
        IBox? b = FindBox(Selection[^1]);
        return b is Node or Group or TextBox { IsComment: false } ? b : null;
    }

    /// <param name="lockAngle">
    /// Placed by the pointer (palette drop, port drag): when joined, put the new shape on an allowed angle from the
    /// source's centre (<see cref="AngleLock"/>). Steps placed by the editor itself (below the selection) pass false.
    /// </param>
    /// <param name="centre">Pan the view so the new shape is in the middle (quick-add: sidebar click, number keys,
    /// "+ Next step"). Shapes placed with the pointer are already where the user is looking.</param>
    /// <remarks>The new shape's label is opened for editing straight away, all text selected, so typing replaces it.</remarks>
    public void AddNodeAt(PaletteItem item, double cx, double cy, bool join, string? groupId = null, bool lockAngle = false, bool centre = false)
    {
        Change();
        IBox? src = join ? JoinSource() : null;
        Node n;
        if (lockAngle && src is not null)
        {
            (double lx, double ly) = LockPoint(src.X + src.W / 2, src.Y + src.H / 2, cx, cy);
            n = CreateNode(item, lx, ly, exact: true);
        }
        else n = CreateNode(item, cx, cy);
        if (src is not null) AddEdge(src.Id, n.Id);
        if (groupId is not null && FindGroup(groupId) is { } g) { n.GroupId = g.Id; GrowGroup(g); }
        else UpdateMembership(new[] { n });
        SelectOnly(n.Id);
        if (centre) view.CentreOn(n.X + n.W / 2, n.Y + n.H / 2);
        BeginEdit(n.Id, checkpoint: false);
    }

    /// <summary>Palette click or number key: place the next step after the selection in the flow direction
    /// (<see cref="Diagram.Flow"/>), joined to it when Auto-join is on.</summary>
    public void QuickAdd(PaletteItem item) => QuickAdd(item, join: AutoConnect);

    /// <summary>The panel's "+ Next step" button: always joined to the selection (that's what the button is for).</summary>
    public void AddNextStep() => QuickAdd(NodeType.Process, join: true);

    public void QuickAdd(PaletteItem item, bool join)
    {
        IBox? src = JoinSource();
        (double tw, double th) = NodeFactory.SizeFor(item);
        NodeTypeInfo t = NodeTypes.Get(item.Type) with { W = tw, H = th };
        double cx, cy;
        if (src is not null)
        {
            (cx, cy) = NextSpot(src, t.W, t.H);
            // Step across the flow until the spot is free (e.g. second branch of a decision).
            ILaneLayout flow = FlowLayout;
            (double _, double stepAcross) = flow.ToLane(t.W + 40, t.H + 40);
            int guard = 0;
            while (Diagram.Nodes.Any(o => Boxes.Intersects(o, cx - t.W / 2 - 10, cy - t.H / 2 - 10, cx + t.W / 2 + 10, cy + t.H / 2 + 10)) && guard++ < 20)
            {
                (double f, double c) = flow.ToLane(cx, cy);
                (cx, cy) = flow.FromLane(f, c + stepAcross);
            }
        }
        else
        {
            (cx, cy) = view.ViewCentre();
            while (Diagram.Nodes.Any(o => Boxes.Contains(o, cx, cy))) cy += 100;
        }
        // A step added from a shape inside a group stays in that group.
        AddNodeAt(item, cx, cy, join: join && src is not null, groupId: (src as IGroupable)?.GroupId, centre: true);
    }

    /// <summary>The flow direction as a lane layout, so placement can work in (flow, cross) coordinates.</summary>
    public ILaneLayout FlowLayout => LaneLayouts.ForFlow(Diagram.Flow);

    /// <summary>Gap between a step and the next one quick-added after it, along the flow.</summary>
    const double NextStepGap = 70;

    /// <summary>
    /// Centre for a new item <paramref name="w"/> × <paramref name="h"/> placed after <paramref name="src"/> in the flow
    /// direction: past the source's far edge (text under a shape counts when the flow runs down), on its centre line.
    /// </summary>
    (double X, double Y) NextSpot(IBox src, double w, double h)
    {
        ILaneLayout flow = FlowLayout;
        (double _, double centreAcross) = flow.ToLane(src.X + src.W / 2, src.Y + src.H / 2);
        (double farEdge, double _) = flow.ToLane(src.X + src.W, src.Y + src.H + TextLayout.ExtraBelow(src));
        (double halfAlong, double _) = flow.ToLane(w / 2, h / 2);
        return flow.FromLane(farEdge + NextStepGap + halfAlong, centreAcross);
    }

    /// <summary>The shape a quick branch would start from: exactly one shape selected (not an outcome); otherwise null.</summary>
    public Node? BranchSource =>
        Selection.Count == 1 && FindBox(Selection[0]) is Node { Kind.IsStep: true } n ? n : null;

    /// <summary>
    /// Quick branch from the selected shape: a Pass and a Fail outcome at 45° either side of the flow (left → right:
    /// Pass above, Fail below; top → down: Pass left, Fail right), each followed by a new process step straight on
    /// along the flow. The two new steps run in parallel. All four join the source's group. One undo step; the Pass
    /// branch's step is selected with its label open for typing, and the view centres on the branch.
    /// </summary>
    public void QuickBranch()
    {
        if (BranchSource is not { } src) { view.ShowToast("Select one shape to branch from"); return; }
        Change();
        ILaneLayout flow = FlowLayout;
        PaletteItem passItem = new(NodeType.Outcome, OutcomeType.Pass), failItem = new(NodeType.Outcome, OutcomeType.Fail);
        NodeTypeInfo outcomeType = NodeTypes.Get(NodeType.Outcome), stepType = NodeTypes.Get(NodeType.Process);

        // Pass / Fail: the same distance along the flow and across it, so they sit on the 45° lines.
        (double srcFlow, double srcCross) = flow.ToLane(src.X + src.W / 2, src.Y + src.H / 2);
        (double srcFar, double _) = flow.ToLane(src.X + src.W, src.Y + src.H + TextLayout.ExtraBelow(src));
        (double outcomeHalf, double _) = flow.ToLane(outcomeType.W / 2, outcomeType.H / 2);
        double offset = srcFar - srcFlow + NextStepGap + outcomeHalf;

        Node pass = AddBranchNode(passItem, flow, srcFlow + offset, srcCross - offset, src);
        Node fail = AddBranchNode(failItem, flow, srcFlow + offset, srcCross + offset, src);
        // Leave the source on the side facing each branch and come into the outcome from the flow side, so the line
        // turns once and stays clear of the outcome's label.
        Side outOfFlow = SideToward(flow.FromLane(1, 0));
        Side intoFlow = SideToward(flow.FromLane(-1, 0));
        AddBranchEdge(src, pass, outOfFlow, intoFlow);
        AddBranchEdge(src, fail, outOfFlow, intoFlow);

        // The next steps: straight on along the flow from each outcome (90° to it).
        (double stepHalf, double _) = flow.ToLane(stepType.W / 2, stepType.H / 2);
        Node passNext = AddBranchNode(NodeType.Process, flow, NextAlong(flow, pass) + stepHalf, srcCross - offset, src);
        Node failNext = AddBranchNode(NodeType.Process, flow, NextAlong(flow, fail) + stepHalf, srcCross + offset, src);
        AddEdge(pass.Id, passNext.Id);
        AddEdge(fail.Id, failNext.Id);
        Steps.MakeParallel(new[] { passNext, failNext });

        if (FindGroup(src.GroupId) is { } g) GrowGroup(g);
        SelectOnly(passNext.Id);
        (double midX, double midY) = flow.FromLane((srcFlow + NextAlong(flow, pass) + stepHalf) / 2, srcCross);
        view.CentreOn(midX, midY);
        BeginEdit(passNext.Id, checkpoint: false);
    }

    /// <summary>Joins two quick-branch nodes with the connector pinned to the given sides.</summary>
    void AddBranchEdge(Node from, Node to, Side fromSide, Side toSide)
    {
        if (!AddEdge(from.Id, to.Id)) return;
        Edge e = Diagram.Edges[^1];
        e.FromSide = fromSide;
        e.ToSide = toSide;
    }

    /// <summary>The side of a shape that faces the unit direction (dx, dy), e.g. (0, -1) → top.</summary>
    static Side SideToward((double X, double Y) dir) => Sides.All.First(side => Sides.Dir(side) == dir);

    /// <summary>A new node for a quick branch, centred on (flow, cross), in the same group as <paramref name="src"/>.</summary>
    Node AddBranchNode(PaletteItem item, ILaneLayout flow, double along, double across, Node src)
    {
        (double cx, double cy) = flow.FromLane(along, across);
        Node n = CreateNode(item, cx, cy);
        n.GroupId = src.GroupId;
        return n;
    }

    /// <summary>Where the next item along the flow may start after <paramref name="b"/>: its far edge (text under it
    /// counts when the flow runs down) plus the usual gap.</summary>
    double NextAlong(ILaneLayout flow, IBox b)
    {
        (double far, double _) = flow.ToLane(b.X + b.W, b.Y + b.H + TextLayout.ExtraBelow(b));
        return far + NextStepGap;
    }

    /// <summary>Adds a text box (or a comment) with its top-left at (x, y), optionally joined/pinned to an item.</summary>
    public void AddTextAt(double x, double y, string? joinTo, bool comment)
    {
        Change();
        TextBox t = comment ? TextBox.NewComment() : new TextBox();
        t.X = SnapV(x); t.Y = SnapV(y);
        Diagram.TextBoxes.Add(t);
        // Comments point at what they annotate; plain text continues the flow from the selection.
        if (joinTo is not null) { if (comment) AddEdge(t.Id, joinTo); else AddEdge(joinTo, t.Id); }
        UpdateMembership(new[] { t });
        SelectOnly(t.Id);
        BeginEdit(t.Id, checkpoint: false);
    }

    public void AddCommentQuick()
    {
        IBox? src = Selection.Count > 0 ? FindBox(Selection[^1]) : null;
        if (src is TextBox { IsComment: true }) src = null;
        if (src is not null) AddTextAt(src.X + src.W + 50, src.Y - 30, src.Id, comment: true);
        else { (double cx, double cy) = view.ViewCentre(); AddTextAt(cx - 100, cy - 55, null, comment: true); }
    }

    public void AddTextQuick()
    {
        IBox? src = JoinSource();
        TextBox t = new TextBox();
        if (src is not null && AutoConnect)
        {
            (double cx, double cy) = NextSpot(src, t.W, t.H);
            AddTextAt(cx - t.W / 2, cy - t.H / 2, src.Id, comment: false);
            view.CentreOn(cx, cy);
        }
        else { (double cx, double cy) = view.ViewCentre(); AddTextAt(cx - t.W / 2, cy - t.H / 2, null, comment: false); }
    }

    public void AddGroupAt(double cx, double cy)
    {
        Change();
        Group g = new Group { Color = NextGroupColour() };
        g.X = SnapV(cx - g.W / 2); g.Y = SnapV(cy - g.H / 2);
        // Dropped inside a group: it becomes a group within that group, which grows to fit.
        Group? outer = GroupAt(cx, cy);
        g.GroupId = outer?.Id;
        Diagram.Groups.Add(g);
        if (outer is not null) GrowGroup(outer);
        SelectOnly(g.Id);
    }

    /// <summary>True when the diagram has nothing in it yet (the lane creator is offered then).</summary>
    public bool IsBlank => Diagram.Nodes.Count == 0 && Diagram.TextBoxes.Count == 0 && Diagram.Groups.Count == 0;

    /// <summary>Default size of a created lane (along the flow, across it) on an open canvas.</summary>
    public const double NewLaneLength = 1200, NewLaneWidth = 240;

    /// <summary>
    /// Starts a diagram with <paramref name="count"/> empty swim lanes (top-level groups "Lane 1", "Lane 2", …) running
    /// the way <paramref name="layout"/> says, <paramref name="gap"/> px apart (0 = edge to edge). With a page size
    /// set they fill the printable area; on an open canvas each lane is <see cref="NewLaneLength"/> ×
    /// <see cref="NewLaneWidth"/>. One undo step. Rename the lanes in the group panel as usual.
    /// The diagram's flow is set to suit the lanes (rows → left to right, columns → top to down).
    /// </summary>
    /// <param name="pageSize">A <see cref="PageSetup"/> size key, or <see cref="PageSetup.None"/> for an open canvas.</param>
    /// <param name="orientation">"portrait" or "landscape" (ignored on an open canvas).</param>
    /// <param name="lockSize">Lock the lanes' size (<see cref="Diagram.LockLanes"/>).</param>
    public void CreateLanes(ILaneLayout layout, int count, double gap, string pageSize, string orientation, bool lockSize)
    {
        Change();
        Diagram.Page.Size = pageSize;
        if (Diagram.Page.Enabled) Diagram.Page.Orientation = orientation;
        Diagram.Flow = layout.Flow;
        Diagram.LockLanes = lockSize;
        count = Math.Clamp(count, 1, 20);
        gap = Math.Clamp(gap, 0, 400);
        double flow = NewLaneLength, across = NewLaneWidth, x0 = 0, y0 = 0;
        if (Diagram.Page.Enabled)
        {
            double m = Diagram.Page.MarginPx;
            (double pageFlow, double pageCross) = layout.ToLane(Diagram.Page.WidthPx - 2 * m, Diagram.Page.HeightPx - 2 * m);
            flow = pageFlow;
            across = Math.Max(Group.Header + 2 * Group.Pad, (pageCross - (count - 1) * gap) / count);
            x0 = m; y0 = m;
        }
        (double w, double h) = layout.FromLane(flow, across);
        for (int i = 0; i < count; i++)
        {
            (double x, double y) = layout.FromLane(0, i * (across + gap));
            Diagram.Groups.Add(new Group { Title = $"Lane {i + 1}", Color = NextGroupColour(), X = x0 + x, Y = y0 + y, W = w, H = h });
        }
        ClearSelection();
    }

    public void AddGroupQuick()
    {
        if (Selection.Any(id => FindBox(id) is IGroupable)) { GroupSelection(); return; }
        (double cx, double cy) = view.ViewCentre();
        AddGroupAt(cx, cy);
    }

    /// <param name="exact">Centre already worked out (e.g. by <see cref="LockPoint"/>): don't snap it again.</param>
    Node CreateNode(PaletteItem item, double cx, double cy, bool exact = false)
    {
        Node n = exact ? NodeFactory.Create(item, cx, cy) : NodeFactory.Create(item, SnapV(cx), SnapV(cy));
        // A new step goes on the end of the flow; reorder it in the panel.
        if (n.Kind.IsStep) n.Step = Steps.Next;
        Diagram.Nodes.Add(n);
        return n;
    }
}
