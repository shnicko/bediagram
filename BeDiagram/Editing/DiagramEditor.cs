using System.Globalization;
using System.Text.Json;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Styles;

namespace BeDiagram.Editing;

/// <summary>
/// The diagram being edited and everything you can do to it: selection, undo / redo (whole-diagram snapshots —
/// the Memento pattern), creating, joining, grouping, aligning, deleting and restyling items.
/// No Blazor, browser or file code: the Designer page shows it and passes user actions in; gestures and commands
/// reach it through the Designer's IEditorContext / ICommandContext. What the editor needs from the screen
/// (a toast, the view centre, focusing the text editor) goes through <see cref="IEditorView"/>.
/// </summary>
public class DiagramEditor
{
    readonly IEditorView view;

    public DiagramEditor(IEditorView view)
    {
        this.view = view;
    }

    // ───────────── State ─────────────

    /// <summary>The open diagram. Replaced by <see cref="Load"/> and by undo / redo.</summary>
    public Diagram Diagram { get; private set; } = new();

    /// <summary>Changed since it was last saved or opened.</summary>
    public bool Dirty { get; set; }

    /// <summary>New shapes join to the selection.</summary>
    public bool AutoConnect { get; set; } = true;

    /// <summary>Snap positions and sizes to a 10 px grid.</summary>
    public bool Snap { get; set; } = true;

    /// <summary>Selected shapes, text boxes and groups, in the order they were picked (so "join" follows that order).</summary>
    public List<string> Selection { get; } = new();

    /// <summary>The selected connector (connectors are selected on their own).</summary>
    public string? SelectedEdgeId { get; set; }

    /// <summary>The item whose text is being edited inline, if any.</summary>
    public string? EditingId { get; set; }

    readonly List<string> undo = new();
    readonly List<string> redo = new();
    const int MaxUndo = 100;
    static readonly JsonSerializerOptions SnapshotJson = new();

    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;

    /// <summary>Starts editing <paramref name="diagram"/>: clean, no history, nothing selected.</summary>
    public void Load(Diagram diagram)
    {
        Diagram = diagram;
        Dirty = false;
        undo.Clear();
        redo.Clear();
        ClearSelection();
        EditingId = null;
    }

    // ───────────── Lookups ─────────────

    public IBox? FindBox(string? id) =>
        id is null ? null
        : (IBox?)Diagram.Nodes.FirstOrDefault(n => n.Id == id)
          ?? (IBox?)Diagram.TextBoxes.FirstOrDefault(c => c.Id == id)
          ?? Diagram.Groups.FirstOrDefault(g => g.Id == id);

    public Group? FindGroup(string? id) => id is null ? null : Diagram.Groups.FirstOrDefault(g => g.Id == id);

    /// <summary>The group hierarchy (Composite) of the open diagram, worked out from the GroupId fields.</summary>
    public GroupTree Tree => new GroupTree(Diagram);

    /// <summary>A group's direct members: shapes, text boxes and the groups nested in it.</summary>
    public IEnumerable<IGroupable> Members(Group g) => Tree.Children(g);

    public Node? SelectedNode => Selection.Count == 1 ? Diagram.Nodes.FirstOrDefault(n => n.Id == Selection[0]) : null;

    public TextBox? SelectedText => Selection.Count == 1 ? Diagram.TextBoxes.FirstOrDefault(c => c.Id == Selection[0]) : null;

    public Group? SelectedGroup => Selection.Count == 1 ? FindGroup(Selection[0]) : null;

    public Edge? SelectedEdge => SelectedEdgeId is null ? null : Diagram.Edges.FirstOrDefault(e => e.Id == SelectedEdgeId);

    public string LabelOf(string id) => FindBox(id) switch
    {
        Node n => string.IsNullOrWhiteSpace(n.Label) ? NodeTypes.Get(n.Type).Display : n.Label,
        TextBox t => t.IsComment ? "Comment" : $"Text “{TextPreview(t.Text)}”",
        Group g => $"Group “{g.Title}”",
        _ => "?",
    };

    static string TextPreview(string text)
    {
        string first = (text ?? "").Replace("\r", "").Split('\n')[0].Trim();
        return first.Length == 0 ? "…" : first.Length > 24 ? first[..24] + "…" : first;
    }

    /// <summary>Top-most item under a world point: shapes first, then text boxes, then groups.</summary>
    public IBox? HitTest(double x, double y, string? exclude = null)
    {
        for (int i = Diagram.Nodes.Count - 1; i >= 0; i--)
            if (Diagram.Nodes[i].Id != exclude && Geometry.Contains(Diagram.Nodes[i], x, y)) return Diagram.Nodes[i];
        for (int i = Diagram.TextBoxes.Count - 1; i >= 0; i--)
            if (Diagram.TextBoxes[i].Id != exclude && Geometry.Contains(Diagram.TextBoxes[i], x, y)) return Diagram.TextBoxes[i];
        // Groups top-most first (inner groups are drawn over their parents), skipping the groups the excluded item is in.
        GroupTree tree = Tree;
        HashSet<string> around = FindBox(exclude) is IGroupable ex ? tree.Ancestors(ex).Select(a => a.Id).ToHashSet() : new HashSet<string>();
        List<Group> order = tree.PaintOrder();
        for (int i = order.Count - 1; i >= 0; i--)
            if (order[i].Id != exclude && !around.Contains(order[i].Id) && Geometry.Contains(order[i], x, y)) return order[i];
        return null;
    }

    /// <summary>The innermost group under a point that <paramref name="item"/> could join (never itself or a group inside it).</summary>
    Group? GroupAt(double x, double y, IGroupable? item = null)
    {
        GroupTree tree = Tree;
        List<Group> order = tree.PaintOrder();
        for (int i = order.Count - 1; i >= 0; i--)
            if (CanHold(tree, order[i], item) && Geometry.Contains(order[i], x, y)) return order[i];
        return null;
    }

    /// <summary>The innermost group that wholly contains group <paramref name="g"/>'s outline (never itself or a group inside it).</summary>
    Group? GroupAround(Group g)
    {
        GroupTree tree = Tree;
        List<Group> order = tree.PaintOrder();
        for (int i = order.Count - 1; i >= 0; i--)
        {
            Group o = order[i];
            if (CanHold(tree, o, g) && g.X >= o.X && g.Y >= o.Y && g.X + g.W <= o.X + o.W && g.Y + g.H <= o.Y + o.H) return o;
        }
        return null;
    }

    /// <summary>A group can't hold itself or one of its own ancestors (that would make a loop).</summary>
    static bool CanHold(GroupTree tree, Group container, IGroupable? item) =>
        item is not Group ig || (container.Id != ig.Id && !tree.IsInside(container, ig));

    public double SnapV(double v) => Snap ? Math.Round(v / 10) * 10 : v;

    /// <summary>Selected ids plus everything inside any selected groups, at any depth (so groups move as a unit).</summary>
    public List<IBox> WithMembers(IEnumerable<string> ids)
    {
        GroupTree tree = Tree;
        List<IBox> result = new List<IBox>();
        HashSet<string> seen = new HashSet<string>();
        foreach (string id in ids)
        {
            IBox? b = FindBox(id);
            if (b is null) continue;
            if (seen.Add(b.Id)) result.Add(b);
            if (b is Group g)
                foreach (IGroupable m in tree.Descendants(g))
                    if (seen.Add(m.Id)) result.Add(m);
        }
        return result;
    }

    // ───────────── Undo / redo (Memento: whole-diagram snapshots) ─────────────

    public string Snapshot() => JsonSerializer.Serialize(Diagram, SnapshotJson);

    public void PushUndo(string snap)
    {
        if (undo.Count > 0 && undo[^1] == snap) return;
        undo.Add(snap);
        if (undo.Count > MaxUndo) undo.RemoveAt(0);
        redo.Clear();
    }

    public void Checkpoint() => PushUndo(Snapshot());

    public void Change() { Checkpoint(); Dirty = true; }

    public void MarkDirty() => Dirty = true;

    void Restore(string snap)
    {
        string name = Diagram.Name;
        Diagram = JsonSerializer.Deserialize<Diagram>(snap, SnapshotJson) ?? new Diagram();
        Diagram.Name = name;
        Selection.RemoveAll(id => FindBox(id) is null);
        if (SelectedEdgeId is not null && SelectedEdge is null) SelectedEdgeId = null;
        EditingId = null;
        Dirty = true;
    }

    public void Undo()
    {
        if (undo.Count == 0) return;
        string current = Snapshot();
        string prev = undo[^1];
        undo.RemoveAt(undo.Count - 1);
        // Skip no-op checkpoints (e.g. a field was focused but not changed).
        if (prev == current && undo.Count > 0) { prev = undo[^1]; undo.RemoveAt(undo.Count - 1); }
        redo.Add(current);
        Restore(prev);
    }

    public void Redo()
    {
        if (redo.Count == 0) return;
        undo.Add(Snapshot());
        string next = redo[^1];
        redo.RemoveAt(redo.Count - 1);
        Restore(next);
    }

    // ───────────── Groups ─────────────

    /// <summary>
    /// After a drag: a loose shape or text box joins the innermost group under its centre; a loose group joins the
    /// innermost group that wholly contains it. A member moves to another group (or out to an outer one) only when
    /// dropped inside that group, otherwise it stays in its own group (which grows to fit, like nudging with the
    /// arrow keys). Leaving the top-level group is done with Ungroup or "remove from group" in the panel.
    /// </summary>
    public void UpdateMembership(IEnumerable<IGroupable> items)
    {
        List<Group> touched = new List<Group>();
        foreach (IGroupable it in items)
        {
            Group? old = Tree.Parent(it);
            Group? target = it is Group ig ? GroupAround(ig) : GroupAt(it.X + it.W / 2, it.Y + it.H / 2, it);
            Group? g = target ?? old;
            it.GroupId = g?.Id;
            if (g is not null && !touched.Contains(g)) touched.Add(g);
            if (old is not null && old != g && !touched.Contains(old)) touched.Add(old);
        }
        foreach (Group g in touched) GrowGroup(g);
    }

    /// <summary>Expands a group (never shrinks it) so all members fit inside with padding, then the groups around it.</summary>
    public void GrowGroup(Group g)
    {
        GrowOne(g);
        foreach (Group outer in Tree.Ancestors(g)) GrowOne(outer);
    }

    /// <summary>Grows the groups around <paramref name="item"/> so it still fits (e.g. after a resize).</summary>
    public void GrowAround(IGroupable item)
    {
        if (Tree.Parent(item) is { } g) GrowGroup(g);
    }

    void GrowOne(Group g)
    {
        List<IBox> members = Members(g).Cast<IBox>().ToList();
        if (members.Count == 0) return;
        (double x, double y, double w, double h) = Geometry.GroupBounds(members, g);
        double right = Math.Max(g.X + g.W, x + w), bottom = Math.Max(g.Y + g.H, y + h);
        g.X = Math.Min(g.X, x); g.Y = Math.Min(g.Y, y);
        g.W = right - g.X; g.H = bottom - g.Y;
    }

    /// <summary>Shrink-wraps a group around its members (the groups around it grow if they need to).</summary>
    public void FitGroup(Group g)
    {
        List<IBox> members = Members(g).Cast<IBox>().ToList();
        if (members.Count == 0) return;
        (g.X, g.Y, g.W, g.H) = Geometry.GroupBounds(members, g);
        foreach (Group outer in Tree.Ancestors(g)) GrowOne(outer);
    }

    string NextGroupColour()
    {
        HashSet<string> used = Diagram.Groups.Select(g => g.Color).ToHashSet();
        return Palette.All.Keys.FirstOrDefault(k => !used.Contains(k)) ?? Palette.All.Keys.ElementAt(Diagram.Groups.Count % Palette.All.Count);
    }

    /// <summary>
    /// Wraps the selected shapes, text boxes and groups in a new group. Items inside another selected group stay
    /// in it (they move with it). The new group sits inside the innermost group that held all the items, so
    /// grouping things inside a group makes a nested group.
    /// </summary>
    public void GroupSelection()
    {
        GroupTree tree = Tree;
        List<IGroupable> picked = Selection.Select(FindBox).OfType<IGroupable>().ToList();
        List<IGroupable> items = picked.Where(i => !tree.Ancestors(i).Any(a => picked.Contains(a))).ToList();
        if (items.Count == 0) { view.ShowToast("Select shapes, text boxes or groups to group"); return; }
        Change();
        Group? outer = tree.CommonParent(items);
        Group g = new Group { Title = "Group", Color = NextGroupColour(), GroupId = outer?.Id };
        (g.X, g.Y, g.W, g.H) = Geometry.GroupBounds(items);
        Diagram.Groups.Add(g);
        foreach (IGroupable it in items) it.GroupId = g.Id;
        if (outer is not null) GrowGroup(outer);
        SelectOnly(g.Id);
        BeginEdit(g.Id, checkpoint: false);
    }

    /// <summary>Removes a group; its members move up into the group around it (or to the top level).</summary>
    public void Ungroup(Group g)
    {
        Change();
        string? outer = Tree.Parent(g)?.Id;
        foreach (IGroupable m in Members(g).ToList()) m.GroupId = outer;
        Diagram.Edges.RemoveAll(e => e.From == g.Id || e.To == g.Id);
        Diagram.Groups.Remove(g);
        ClearSelection();
    }

    /// <summary>Takes an item out of its group, into the group around that one (or to the top level).</summary>
    public void RemoveFromGroup(IGroupable item)
    {
        GroupTree tree = Tree;
        if (tree.Parent(item) is not { } g) return;
        Change();
        Group? outer = tree.Parent(g);
        item.GroupId = outer?.Id;
        // Move it just outside the group so it doesn't look like it's still inside.
        MoveBox(item, SnapV(g.X + g.W + 40), item.Y);
        FitGroup(g);
        if (outer is not null) GrowGroup(outer);
    }

    /// <summary>Moves a box; a group carries everything inside it (at any depth) with it.</summary>
    public void MoveBox(IBox b, double x, double y)
    {
        double dx = x - b.X, dy = y - b.Y;
        b.X = x; b.Y = y;
        if (b is Group g)
            foreach (IGroupable m in Tree.Descendants(g)) { m.X += dx; m.Y += dy; }
    }

    // ───────────── Alignment ─────────────

    /// <summary>
    /// Aligns/arranges items. Inside a group the group's interior is the reference; otherwise the selection bounds.
    /// Left/centre/right/top/middle/bottom line up the shape outlines only — a label drawn under a circle or outcome
    /// is ignored, so a circle and a box aligned to the bottom share the same bottom edge.
    /// Distribute and column still leave room for those labels so they don't run into the next item.
    /// </summary>
    public void Align(AlignOp op, List<IBox> items, Group? within)
    {
        if (items.Count == 0 || (within is null && items.Count < 2)) return;
        Change();
        double L, T, R, B;
        if (within is not null)
        {
            L = within.X + Group.Pad; R = within.X + within.W - Group.Pad;
            T = within.Y + Geometry.GroupInsetTop(within) + Group.Pad;
            // Leave room inside the group for a bottom title band and for the tallest label hanging below an outline.
            B = within.Y + within.H - Group.Pad - Geometry.GroupInsetBottom(within) - items.Max(Geometry.ExtraBelow);
        }
        else
        {
            L = items.Min(b => b.X); R = items.Max(b => b.X + b.W);
            T = items.Min(b => b.Y); B = items.Max(b => b.Y + b.H);
        }
        const double gap = 40;

        switch (op)
        {
            case AlignOp.Left: foreach (IBox b in items) MoveBox(b, L, b.Y); break;
            case AlignOp.CenterX: foreach (IBox b in items) MoveBox(b, (L + R) / 2 - b.W / 2, b.Y); break;
            case AlignOp.Right: foreach (IBox b in items) MoveBox(b, R - b.W, b.Y); break;
            case AlignOp.Top: foreach (IBox b in items) MoveBox(b, b.X, T); break;
            case AlignOp.Middle: foreach (IBox b in items) MoveBox(b, b.X, (T + B) / 2 - b.H / 2); break;
            case AlignOp.Bottom: foreach (IBox b in items) MoveBox(b, b.X, B - b.H); break;
            case AlignOp.DistributeH:
            {
                List<IBox> sorted = items.OrderBy(b => b.X + b.W / 2).ToList();
                double space = (R - L) - sorted.Sum(b => b.W);
                double g = sorted.Count > 1 ? space / (sorted.Count - 1) : 0;
                if (g < 20) g = gap;
                double x = sorted.Count > 1 ? L : (L + R) / 2 - sorted[0].W / 2;
                foreach (IBox b in sorted) { MoveBox(b, x, b.Y); x += b.W + g; }
                break;
            }
            case AlignOp.DistributeV:
            {
                // First outline top stays at T, last outline bottom at B; gaps are measured from the bottom of each
                // item's label (if any) to the top of the next outline.
                List<IBox> sorted = items.OrderBy(b => b.Y + b.H / 2).ToList();
                double space = (B - T) - sorted.Sum(b => b.H) - sorted.Take(sorted.Count - 1).Sum(Geometry.ExtraBelow);
                double g = sorted.Count > 1 ? space / (sorted.Count - 1) : 0;
                if (g < 20) g = gap;
                double y = sorted.Count > 1 ? T : (T + B) / 2 - sorted[0].H / 2;
                foreach (IBox b in sorted) { MoveBox(b, b.X, y); y += b.H + Geometry.ExtraBelow(b) + g; }
                break;
            }
            case AlignOp.Row:
            {
                List<IBox> sorted = items.OrderBy(b => b.X).ToList();
                double tallest = sorted.Max(b => b.H);
                double mid = within is not null ? T + tallest / 2 : (T + B) / 2;
                double x = L;
                foreach (IBox b in sorted) { MoveBox(b, SnapV(x), mid - b.H / 2); x += b.W + gap; }
                break;
            }
            case AlignOp.Column:
            {
                List<IBox> sorted = items.OrderBy(b => b.Y).ToList();
                double widest = sorted.Max(b => b.W);
                double mid = within is not null ? L + widest / 2 : (L + R) / 2;
                double y = T;
                foreach (IBox b in sorted) { MoveBox(b, mid - b.W / 2, SnapV(y)); y += b.H + Geometry.ExtraBelow(b) + gap; }
                break;
            }
        }

        if (within is not null)
        {
            if (op is AlignOp.Row or AlignOp.Column) FitGroup(within); else GrowGroup(within);
        }
        else
        {
            GroupTree tree = Tree;
            foreach (Group g in items.OfType<IGroupable>().Select(tree.Parent).OfType<Group>().Distinct().ToList())
                GrowGroup(g);
        }
    }

    public void AlignSelection(AlignOp op) => Align(op, Selection.Select(FindBox).Where(b => b is not null).Cast<IBox>().ToList(), null);

    public void AlignInGroup(Group g, AlignOp op) => Align(op, Members(g).Cast<IBox>().ToList(), g);

    // ───────────── Creating items ─────────────

    public bool AddEdge(string from, string to, string label = "")
    {
        if (from == to) return false;
        if (Diagram.Edges.Any(e => (e.From == from && e.To == to))) return false;
        IBox? a = FindBox(from); IBox? b = FindBox(to);
        if (a is null || b is null) return false;
        // Two comments pinned to each other is meaningless; any other pairing (text ↔ shape, text ↔ text) is fine.
        bool aNote = a is TextBox { IsComment: true }, bNote = b is TextBox { IsComment: true };
        if (aNote && bNote) return false;
        // A group connecting to something inside it (at any depth) is meaningless.
        GroupTree tree = Tree;
        if (a is Group ga && b is IGroupable ib && tree.IsInside(ib, ga)) return false;
        if (b is Group gb && a is IGroupable ia && tree.IsInside(ia, gb)) return false;
        Edge edge = new Edge { From = from, To = to, Label = label };
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

    public void AddNodeAt(string type, double cx, double cy, bool join, string? groupId = null)
    {
        Change();
        IBox? src = join ? JoinSource() : null;
        Node n = CreateNode(type, cx, cy);
        if (src is not null) AddEdge(src.Id, n.Id);
        if (groupId is not null && FindGroup(groupId) is { } g) { n.GroupId = g.Id; GrowGroup(g); }
        else UpdateMembership(new[] { n });
        SelectOnly(n.Id);
    }

    /// <summary>Palette click or number key: place the next step below the selection, joined to it when Auto-join is on.</summary>
    public void QuickAdd(string type) => QuickAdd(type, join: AutoConnect);

    /// <summary>The panel's "+ Next step" button: always joined to the selection (that's what the button is for).</summary>
    public void AddNextStep() => QuickAdd(NodeTypes.Process, join: true);

    public void QuickAdd(string type, bool join)
    {
        IBox? src = JoinSource();
        NodeTypeInfo t = NodeTypes.Get(NodeFactory.ParsePaletteKey(type).Type);
        double cx, cy;
        if (src is not null)
        {
            cx = src.X + src.W / 2;
            cy = src.Y + src.H + Geometry.ExtraBelow(src) + 70 + t.H / 2;
            // Step sideways until the spot is free (e.g. second branch of a decision).
            int guard = 0;
            while (Diagram.Nodes.Any(o => Geometry.Intersects(o, cx - t.W / 2 - 10, cy - t.H / 2 - 10, cx + t.W / 2 + 10, cy + t.H / 2 + 10)) && guard++ < 20)
                cx += t.W + 40;
        }
        else
        {
            (cx, cy) = view.ViewCentre();
            while (Diagram.Nodes.Any(o => Geometry.Contains(o, cx, cy))) cy += 100;
        }
        // A step added from a shape inside a group stays in that group.
        AddNodeAt(type, cx, cy, join: join && src is not null, groupId: (src as IGroupable)?.GroupId);
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
            AddTextAt(src.X + src.W / 2 - t.W / 2, src.Y + src.H + Geometry.ExtraBelow(src) + 60, src.Id, comment: false);
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

    public void AddGroupQuick()
    {
        if (Selection.Any(id => FindBox(id) is IGroupable)) { GroupSelection(); return; }
        (double cx, double cy) = view.ViewCentre();
        AddGroupAt(cx, cy);
    }

    Node CreateNode(string key, double cx, double cy)
    {
        Node n = NodeFactory.Create(key, SnapV(cx), SnapV(cy));
        Diagram.Nodes.Add(n);
        return n;
    }

    // ───────────── Selection ─────────────

    public void SelectOnly(string id) { Selection.Clear(); Selection.Add(id); SelectedEdgeId = null; }

    public void ClearSelection() { Selection.Clear(); SelectedEdgeId = null; }

    public void SelectAll()
    {
        Selection.Clear();
        Selection.AddRange(Diagram.Nodes.Select(n => n.Id));
        Selection.AddRange(Diagram.TextBoxes.Select(c => c.Id));
        Selection.AddRange(Diagram.Groups.Select(g => g.Id));
        SelectedEdgeId = null;
    }

    // ───────────── Connector routes ─────────────

    public void ResetRoute(Edge e)
    {
        if (!e.IsManual) return;
        Change();
        e.ResetRoute();
    }

    public void ResetBend(string id)
    {
        Edge? edge = Diagram.Edges.FirstOrDefault(x => x.Id == id);
        if (edge is null || edge.Bend == 0) return;
        Change();
        edge.Bend = 0;
    }

    // ───────────── Editing actions ─────────────

    public void JoinSelected()
    {
        if (Selection.Count < 2) { view.ShowToast("Select two or more items to join"); return; }
        string snap = Snapshot();
        int added = 0;
        for (int i = 0; i < Selection.Count - 1; i++)
            if (AddEdge(Selection[i], Selection[i + 1])) added++;
        if (added > 0) { PushUndo(snap); Dirty = true; }
        view.ShowToast(added == 0 ? "Already joined" : $"Joined {added} connector{(added == 1 ? "" : "s")}");
    }

    public void DeleteSelection()
    {
        if (Selection.Count == 0 && SelectedEdgeId is null) return;
        Change();
        if (SelectedEdgeId is not null) Diagram.Edges.RemoveAll(e => e.Id == SelectedEdgeId);
        // Deleting a group deletes its contents too (Ungroup keeps them).
        HashSet<string> ids = WithMembers(Selection).Select(b => b.Id).ToHashSet();
        Diagram.Nodes.RemoveAll(n => ids.Contains(n.Id));
        Diagram.TextBoxes.RemoveAll(c => ids.Contains(c.Id));
        Diagram.Groups.RemoveAll(g => ids.Contains(g.Id));
        Diagram.Edges.RemoveAll(e => ids.Contains(e.From) || ids.Contains(e.To));
        ClearSelection();
        EditingId = null;
    }

    public void Duplicate()
    {
        if (Selection.Count == 0) return;
        Change();
        Dictionary<string, string> map = new Dictionary<string, string>();
        List<IBox> items = WithMembers(Selection);
        // Groups first so members (and nested groups) can be re-pointed at the copies.
        List<(Group Old, Group Copy)> groupCopies = new List<(Group Old, Group Copy)>();
        foreach (Group g in items.OfType<Group>())
        {
            Group gg = new Group { Title = g.Title, Color = g.Color, X = g.X + 30, Y = g.Y + 30, W = g.W, H = g.H };
            Diagram.Groups.Add(gg); map[g.Id] = gg.Id;
            groupCopies.Add((g, gg));
        }
        string? NewGroup(string? old) => old is not null && map.TryGetValue(old, out string? ng) ? ng : old;
        foreach ((Group g, Group gg) in groupCopies) gg.GroupId = NewGroup(g.GroupId);
        foreach (IBox b in items)
        {
            switch (b)
            {
                case Node n:
                    Node nn = new Node
                    {
                        Type = n.Type, Label = n.Label, X = n.X + 30, Y = n.Y + 30, W = n.W, H = n.H,
                        Icon = n.Icon, Color = n.Color, Filled = n.Filled, Outcome = n.Outcome, GroupId = NewGroup(n.GroupId),
                        Description = n.Description, TextWidth = n.TextWidth, ClearTextBack = n.ClearTextBack,
                        Properties = new(n.Properties),
                    };
                    Diagram.Nodes.Add(nn); map[n.Id] = nn.Id; break;
                case TextBox c:
                    TextBox cc = c.Copy();
                    cc.X += 30; cc.Y += 30; cc.GroupId = NewGroup(c.GroupId);
                    Diagram.TextBoxes.Add(cc); map[c.Id] = cc.Id; break;
            }
        }
        foreach (Edge e in Diagram.Edges.ToList())
            if (map.TryGetValue(e.From, out string? f) && map.TryGetValue(e.To, out string? t))
            {
                Edge copy = new Edge { From = f, To = t, Label = e.Label, LabelPos = e.LabelPos };
                copy.CopyLookFrom(e);
                copy.CopyRouteFrom(e);
                Diagram.Edges.Add(copy);
            }
        List<string> newSel = Selection.Where(map.ContainsKey).Select(id => map[id]).ToList();
        Selection.Clear();
        Selection.AddRange(newSel);
    }

    public void Nudge(double dx, double dy)
    {
        if (Selection.Count == 0) return;
        Change();
        List<IBox> moved = WithMembers(Selection);
        HashSet<string> movedIds = moved.Select(b => b.Id).ToHashSet();
        foreach (IBox b in moved) { b.X += dx; b.Y += dy; }
        foreach (Group g in Diagram.Groups.Where(g => !movedIds.Contains(g.Id) && Members(g).Any(m => movedIds.Contains(m.Id))).ToList())
            GrowGroup(g);
    }

    // ───────────── Page ─────────────

    /// <summary>Items not fully inside the page (null when there is no page).</summary>
    public List<IBox> OutsidePage()
    {
        if (!Diagram.Page.Enabled) return new();
        double W = Diagram.Page.WidthPx, H = Diagram.Page.HeightPx;
        return Diagram.Nodes.Cast<IBox>().Concat(Diagram.TextBoxes).Concat(Diagram.Groups)
            .Where(b => b.X < -0.5 || b.Y < -0.5 || b.X + b.W > W + 0.5 || b.Y + b.H + Geometry.ExtraBelow(b) > H + 0.5)
            .ToList();
    }

    public bool ContentTooBigForPage()
    {
        if (!Diagram.Page.Enabled || Geometry.ContentBounds(Diagram) is not { } cb) return false;
        double m = Diagram.Page.MarginPx;
        return cb.W > Diagram.Page.WidthPx - 2 * m + 0.5 || cb.H > Diagram.Page.HeightPx - 2 * m + 0.5;
    }

    public void CentreOnPage() => CentreOnPage(checkpoint: true);

    /// <summary>Moves the whole drawing so it is centred inside the page margins.</summary>
    public void CentreOnPage(bool checkpoint)
    {
        if (!Diagram.Page.Enabled || Geometry.ContentBounds(Diagram) is not { } cb) return;
        if (checkpoint) Change();
        double W = Diagram.Page.WidthPx, H = Diagram.Page.HeightPx, m = Diagram.Page.MarginPx;
        double dx = SnapV(m + (W - 2 * m - cb.W) / 2 - cb.X);
        double dy = SnapV(m + (H - 2 * m - cb.H) / 2 - cb.Y);
        foreach (IBox b in Diagram.Nodes.Cast<IBox>().Concat(Diagram.TextBoxes).Concat(Diagram.Groups)) { b.X += dx; b.Y += dy; }
        if (checkpoint && ContentTooBigForPage())
            view.ShowToast("Centred — but the drawing is bigger than the printable area");
    }

    public void SelectOutsidePage()
    {
        Selection.Clear(); SelectedEdgeId = null;
        Selection.AddRange(OutsidePage().Select(b => b.Id));
    }

    public void PageChanged() { Dirty = true; }

    // ───────────── Connector style & labels ─────────────

    public void SetDefaultConnector(string style)
    {
        Diagram.DefaultConnector = style;
        Dirty = true;
        view.ShowToast($"New connectors will be {style}" + (SelectedEdge is null ? "" : " — use the panel to change the selected one"));
    }

    public void SetEdgeStyle(Edge e, string style) { if (e.Style == style) return; Change(); e.Style = style; }

    public void SetLabelPlace(Edge e, string place) { if (e.LabelPlace == place) return; Change(); e.LabelPlace = place; }

    /// <summary>Panel label box: keeps the text to two lines (e.g. after a paste).</summary>
    public void EdgeLabelTyped()
    {
        if (SelectedEdge is { } e) e.Label = Geometry.ClampEdgeLabel(e.Label);
        MarkDirty();
    }

    public void SetLabelOrient(Edge e, string orient) { if (e.LabelOrient == orient) return; Change(); e.LabelOrient = orient; }

    public void SetLabelPos(Edge e, double pos) { Change(); e.LabelPos = pos; }

    public void DragLabelPos(Edge e, object? value)
    {
        if (double.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
        {
            e.LabelPos = Math.Clamp(v, 0, 1);
            Dirty = true;
        }
    }

    public void ApplyEdgeLookToAll(Edge src)
    {
        Change();
        foreach (Edge e in Diagram.Edges) e.CopyLookFrom(src);
        Diagram.DefaultLook.CopyLookFrom(src);
        Diagram.DefaultConnector = src.Style;
        view.ShowToast($"Applied to {Diagram.Edges.Count} connectors");
    }

    public void SetEdgeLook(Edge e, Action<Edge> change) { Change(); change(e); }

    public void MakeDefaultLook(Edge src)
    {
        Dirty = true;
        Diagram.DefaultLook.CopyLookFrom(src);
        Diagram.DefaultConnector = src.Style;
        view.ShowToast("New connectors will use this look");
    }

    public void SetStyleBetweenSelected(string style)
    {
        HashSet<string> ids = WithMembers(Selection).Select(b => b.Id).ToHashSet();
        List<Edge> edges = Diagram.Edges.Where(e => ids.Contains(e.From) && ids.Contains(e.To)).ToList();
        if (edges.Count == 0) { view.ShowToast("No connectors between the selected items"); return; }
        Change();
        foreach (Edge e in edges) e.Style = style;
        view.ShowToast($"{edges.Count} connector{(edges.Count == 1 ? "" : "s")} set to {style}");
    }

    public void ReverseEdge(Edge e)
    {
        Change();
        (e.From, e.To) = (e.To, e.From);
        (e.FromSide, e.ToSide) = (e.ToSide, e.FromSide);
    }

    public void SetEdgeLabel(Edge e, string label) { Checkpoint(); e.Label = label; MarkDirty(); }

    // ───────────── Shapes and groups ─────────────

    public void ChangeType(Node n, string? type)
    {
        if (type is null || type == n.Type) return;
        Change();
        NodeTypeInfo oldT = NodeTypes.Get(n.Type);
        NodeTypeInfo t = NodeTypes.Get(type);
        string oldDisplay = n.Type == NodeTypes.Outcome ? Outcomes.Get(n.Outcome).Display : oldT.Display;
        if (t.Key == NodeTypes.Outcome) { n.Outcome ??= Outcomes.Pass; n.Filled = true; }
        else n.Outcome = null;
        if (n.Label == oldDisplay) n.Label = t.Key == NodeTypes.Outcome ? Outcomes.Get(n.Outcome).Display : t.Display;
        double cx = n.X + n.W / 2; double cy = n.Y + n.H / 2;
        n.Type = t.Key; n.W = t.W; n.H = t.H;
        n.X = SnapV(cx) - t.W / 2; n.Y = SnapV(cy) - t.H / 2;
        if (t.Key == NodeTypes.Circle && n.Icon is null) n.Icon = "layers";
        foreach ((string k, string v) in NodeFactory.DefaultProperties(t.Key)) n.Properties.TryAdd(k, v);
    }

    /// <summary>Description or text width edited: the text under the shape changed size, so its group may need to grow.</summary>
    public void OutsideTextChanged(Node n)
    {
        MarkDirty();
        if (FindGroup(n.GroupId) is { } g) GrowGroup(g);
    }

    public void SetNodeColour(Node n, string? colour) { Change(); n.Color = colour; }

    public void SetFilled(Node n, bool filled) { Change(); n.Filled = filled; }
    public void SetClearTextBack(Node n, bool clear) { Change(); n.ClearTextBack = clear; }

    public void SetOutcome(Node n, string outcome)
    {
        if (n.Outcome == outcome) return;
        Change();
        // A label still showing the old outcome's name follows the new one.
        if (n.Label == Outcomes.Get(n.Outcome).Display) n.Label = Outcomes.Get(outcome).Display;
        n.Outcome = outcome;
    }

    public void SetIcon(Node n, string? icon) { Change(); n.Icon = icon; }

    public void SetGroupColour(Group g, string colour) { Change(); g.Color = colour; }

    public void SetGroupFilled(Group g, bool filled) { Change(); g.Filled = filled; }

    public void SetGroupBorder(Group g, string pattern) { Change(); g.Border = pattern; }

    public void SetGroupBorderWeight(Group g, string weight) { Change(); g.BorderWeight = weight; }

    /// <summary>Moving or restyling the title changes the room it takes inside the group, so the group grows if needed.</summary>
    public void SetGroupTitleLook(Group g, string? pos, string? style)
    {
        Change();
        if (pos is not null) g.HeaderPos = pos;
        if (style is not null) g.HeaderStyle = style;
        GrowGroup(g);
    }

    public void SetNodeLink(Node n, string? link) { Change(); n.Link = link; }

    public void SetGroupLink(Group g, string? link) { Change(); g.Link = link; }

    public void FitGroupToContents(Group g) { Change(); FitGroup(g); }

    /// <summary>
    /// Moves the group box (same size) so its contents sit in the middle of it. The items stay where they are.
    /// "Middle" allows for the title band and for labels under circles, like <see cref="FitGroup"/>.
    /// </summary>
    public void CentreGroupOnContents(Group g)
    {
        if (ContentsOffset(g) is not { } off) return;
        Change();
        g.X -= off.Dx; g.Y -= off.Dy;   // the group alone, not MoveBox: its members stay put
        foreach (Group outer in Tree.Ancestors(g)) GrowOne(outer);
    }

    /// <summary>Moves the group's contents together (keeping their layout) to the middle of the group. The box stays.</summary>
    public void CentreContentsInGroup(Group g)
    {
        if (ContentsOffset(g) is not { } off) return;
        Change();
        foreach (IBox b in Members(g).Cast<IBox>().ToList()) MoveBox(b, b.X + off.Dx, b.Y + off.Dy);
    }

    /// <summary>
    /// How far the contents would have to move to sit in the middle of the group: the centre of the group minus the
    /// centre of the shrink-wrapped box <see cref="FitGroup"/> would make. Null when the group is empty.
    /// </summary>
    (double Dx, double Dy)? ContentsOffset(Group g)
    {
        List<IBox> members = Members(g).Cast<IBox>().ToList();
        if (members.Count == 0) return null;
        (double x, double y, double w, double h) = Geometry.GroupBounds(members, g);
        return (g.X + g.W / 2 - (x + w / 2), g.Y + g.H / 2 - (y + h / 2));
    }

    // ───────────── Text boxes and properties ─────────────

    public void SetTextLook(TextBox t, Action<TextBox> change) { Change(); change(t); }

    /// <summary>Turning the comment flag on applies the standard comment look; turning it off keeps the look.</summary>
    public void SetComment(TextBox t, bool on)
    {
        if (t.IsComment == on) return;
        Change();
        if (on) t.ApplyCommentStyle(); else t.IsComment = false;
        foreach (Edge e in Diagram.Edges.Where(e => e.From == t.Id || e.To == t.Id))
        {
            if (on) TextBox.ApplyPinLook(e);
            else { e.CopyLookFrom(Diagram.DefaultLook); e.Style = Diagram.DefaultConnector; }
        }
        // Comments can't be pinned to other comments.
        if (on) Diagram.Edges.RemoveAll(e => (e.From == t.Id && FindBox(e.To) is TextBox { IsComment: true }) || (e.To == t.Id && FindBox(e.From) is TextBox { IsComment: true }));
    }

    public void ResetCommentStyle(TextBox t) { Change(); t.ApplyCommentStyle(); }

    /// <summary>Sets the height so the wrapped text just fits.</summary>
    public void FitTextHeight(TextBox t)
    {
        Change();
        t.H = TextHeight(t);
        GrowAround(t);
    }

    /// <summary>
    /// Makes a text box taller if its text no longer fits; never shrinks it (a box made tall on purpose stays tall).
    /// No undo step of its own: it runs after typing, which already has one. The groups around it grow too.
    /// </summary>
    public void GrowToText(TextBox t)
    {
        double h = TextHeight(t);
        if (h <= t.H) return;
        t.H = h;
        Dirty = true;
        GrowAround(t);
    }

    /// <summary>The height at which the wrapped text just fits.</summary>
    static double TextHeight(TextBox t) => Math.Max(24, Math.Ceiling(Geometry.Layout(t).ContentHeight / 2) * 2);

    public void SetFontSize(TextBox t, object? value)
    {
        if (!double.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) return;
        Change();
        t.FontSize = Math.Clamp(v, 6, 96);
    }

    public void AddProp(Node n)
    {
        Change();
        int i = 1;
        while (n.Properties.ContainsKey($"prop{i}")) i++;
        n.Properties[$"prop{i}"] = "";
    }

    public void RenameProp(Node n, string oldKey, string? newKey)
    {
        newKey = newKey?.Trim();
        if (string.IsNullOrEmpty(newKey) || newKey == oldKey || n.Properties.ContainsKey(newKey)) return;
        // Rebuild to keep ordering stable.
        Dictionary<string, string> rebuilt = n.Properties.ToDictionary(kv => kv.Key == oldKey ? newKey : kv.Key, kv => kv.Value);
        n.Properties = rebuilt;
        Dirty = true;
    }

    public void RemoveProp(Node n, string key) { Checkpoint(); n.Properties.Remove(key); }

    // ───────────── Inline text editing ─────────────

    public void BeginEdit(string id) => BeginEdit(id, checkpoint: true);

    public void BeginEdit(string id, bool checkpoint)
    {
        if (checkpoint) Checkpoint();
        EditingId = id;
        if (Diagram.Edges.Any(e => e.Id == id)) { Selection.Clear(); SelectedEdgeId = id; }
        else if (!Selection.Contains(id)) SelectOnly(id);
        view.EditStarted();
    }

    public void EndEdit()
    {
        if (EditingId is null) return;
        if (Diagram.Nodes.FirstOrDefault(n => n.Id == EditingId) is { } n) n.Label = n.Label.TrimEnd('\n', '\r');
        if (Diagram.TextBoxes.FirstOrDefault(t => t.Id == EditingId) is { } tb) GrowToText(tb);
        if (Diagram.Edges.FirstOrDefault(x => x.Id == EditingId) is { } edge) edge.Label = Geometry.ClampEdgeLabel(edge.Label).Trim();
        EditingId = null;
    }
}
