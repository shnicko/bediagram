using System.Globalization;
using System.Text.Json;
using BeDiagram.Models;
using BeDiagram.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BeDiagram.Components.Pages;

public partial class Designer
{
    [Inject] DiagramStore Store { get; set; } = default!;
    [Inject] IJSRuntime JS { get; set; } = default!;

    enum Mode { None, Move, Connect, Pan, Marquee, Resize, EdgeEnd, Bend }
    enum Dialog { None, Open, SaveAs, ConfirmNew, ConfirmLeave }

    // Document state
    Diagram D = new();
    bool Dirty;
    bool hasFile;

    // View state
    double PanX = 40, PanY = 40, Zoom = 1;
    double canvasLeft, canvasTop, canvasW = 1000, canvasH = 700;
    bool AutoConnect = true;
    bool Snap = true;

    // Selection (ordered, so "join" follows the order items were picked)
    readonly List<string> Sel = new();
    string? SelEdge;
    string? EditId;

    // Gesture state
    Mode mode;
    double startX, startY, curX, curY, lastClientX, lastClientY;
    Dictionary<string, (double X, double Y)> moveOrigins = new();
    bool moved;
    string? connectFrom;
    string? resizeId;
    string? pendingGroupSelect;
    string? gestureSnapshot;
    string? paletteDrag;
    bool skipRender;
    // Connector handle drags
    string? edgeDragId;
    bool edgeDragFrom;
    double bendCoord0, bend0;
    bool bendVertical;

    // Undo
    readonly List<string> undo = new();
    readonly List<string> redo = new();
    const int MaxUndo = 100;

    // UI
    Dialog dialog;
    string saveAsName = "";
    string? confirmDelete;
    string? toast;
    string iconFilter = "";
    /// <summary>Saved diagrams listed in the Open dialog (read when it opens).</summary>
    IReadOnlyList<(string Name, DateTime Modified)> openFiles = Array.Empty<(string Name, DateTime Modified)>();
    bool exportMenu;
    CancellationTokenSource? toastCts;

    ElementReference canvasEl;
    IJSObjectReference? module;
    DotNetObjectReference<Designer>? selfRef;

    static readonly JsonSerializerOptions SnapshotJson = new();

    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    // ───────────── Lifecycle ─────────────

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            module = await JS.InvokeAsync<IJSObjectReference>("import", "./js/designer.js");
            selfRef = DotNetObjectReference.Create(this);
            await module.InvokeVoidAsync("init", selfRef, canvasEl);
        }
    }

    protected override bool ShouldRender()
    {
        if (skipRender) { skipRender = false; return false; }
        return true;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (module is not null)
            {
                await module.InvokeVoidAsync("dispose");
                await module.DisposeAsync();
            }
        }
        catch (JSDisconnectedException) { }
        selfRef?.Dispose();
    }

    // ───────────── Lookups ─────────────

    IBox? FindBox(string? id) =>
        id is null ? null
        : (IBox?)D.Nodes.FirstOrDefault(n => n.Id == id)
          ?? (IBox?)D.TextBoxes.FirstOrDefault(c => c.Id == id)
          ?? D.Groups.FirstOrDefault(g => g.Id == id);

    Group? FindGroup(string? id) => id is null ? null : D.Groups.FirstOrDefault(g => g.Id == id);

    IEnumerable<IGroupable> Members(Group g) =>
        D.Nodes.Where(n => n.GroupId == g.Id).Cast<IGroupable>().Concat(D.TextBoxes.Where(c => c.GroupId == g.Id));

    Node? SelectedNode => Sel.Count == 1 ? D.Nodes.FirstOrDefault(n => n.Id == Sel[0]) : null;
    TextBox? SelectedText => Sel.Count == 1 ? D.TextBoxes.FirstOrDefault(c => c.Id == Sel[0]) : null;
    Group? SelectedGroup => Sel.Count == 1 ? FindGroup(Sel[0]) : null;
    Edge? SelectedEdgeObj => SelEdge is null ? null : D.Edges.FirstOrDefault(e => e.Id == SelEdge);

    string LabelOf(string id) => FindBox(id) switch
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
    IBox? HitTest(double x, double y, string? exclude = null)
    {
        for (int i = D.Nodes.Count - 1; i >= 0; i--)
            if (D.Nodes[i].Id != exclude && Geometry.Contains(D.Nodes[i], x, y)) return D.Nodes[i];
        for (int i = D.TextBoxes.Count - 1; i >= 0; i--)
            if (D.TextBoxes[i].Id != exclude && Geometry.Contains(D.TextBoxes[i], x, y)) return D.TextBoxes[i];
        string? ownGroup = (FindBox(exclude) as IGroupable)?.GroupId;
        for (int i = D.Groups.Count - 1; i >= 0; i--)
            if (D.Groups[i].Id != exclude && D.Groups[i].Id != ownGroup && Geometry.Contains(D.Groups[i], x, y)) return D.Groups[i];
        return null;
    }

    Group? GroupAt(double x, double y)
    {
        for (int i = D.Groups.Count - 1; i >= 0; i--)
            if (Geometry.Contains(D.Groups[i], x, y)) return D.Groups[i];
        return null;
    }

    static IEnumerable<(double, double)> Ports(IBox n)
    {
        yield return (n.X + n.W / 2, n.Y);
        yield return (n.X + n.W, n.Y + n.H / 2);
        yield return (n.X + n.W / 2, n.Y + n.H);
        yield return (n.X, n.Y + n.H / 2);
    }

    (double X, double Y) ToWorld(double clientX, double clientY) =>
        ((clientX - canvasLeft - PanX) / Zoom, (clientY - canvasTop - PanY) / Zoom);

    (double X, double Y) ViewCentre() => ((canvasW / 2 - PanX) / Zoom, (canvasH / 2 - PanY) / Zoom);

    double SnapV(double v) => Snap ? Math.Round(v / 10) * 10 : v;

    /// <summary>Selected ids plus the members of any selected groups (so groups move as a unit).</summary>
    List<IBox> WithMembers(IEnumerable<string> ids)
    {
        List<IBox> result = new List<IBox>();
        HashSet<string> seen = new HashSet<string>();
        foreach (string id in ids)
        {
            IBox? b = FindBox(id);
            if (b is null) continue;
            if (seen.Add(b.Id)) result.Add(b);
            if (b is Group g)
                foreach (IGroupable m in Members(g))
                    if (seen.Add(m.Id)) result.Add(m);
        }
        return result;
    }

    // ───────────── Undo / dirty ─────────────

    string Snapshot() => JsonSerializer.Serialize(D, SnapshotJson);

    void PushUndo(string snap)
    {
        if (undo.Count > 0 && undo[^1] == snap) return;
        undo.Add(snap);
        if (undo.Count > MaxUndo) undo.RemoveAt(0);
        redo.Clear();
    }

    void Checkpoint() => PushUndo(Snapshot());

    void Change() { Checkpoint(); Dirty = true; }

    void MarkDirty() => Dirty = true;

    void Restore(string snap)
    {
        string name = D.Name;
        D = JsonSerializer.Deserialize<Diagram>(snap, SnapshotJson) ?? new Diagram();
        D.Name = name;
        Sel.RemoveAll(id => FindBox(id) is null);
        if (SelEdge is not null && SelectedEdgeObj is null) SelEdge = null;
        EditId = null;
        Dirty = true;
    }

    void Undo()
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

    void Redo()
    {
        if (redo.Count == 0) return;
        undo.Add(Snapshot());
        string next = redo[^1];
        redo.RemoveAt(redo.Count - 1);
        Restore(next);
    }

    // ───────────── Groups ─────────────

    /// <summary>
    /// After a drag: a loose item joins the group under its centre; a member moves to another group only when
    /// dropped inside that group, otherwise it stays in its own group (which grows to fit, like nudging with
    /// the arrow keys). Leaving a group is done with Ungroup or "remove from group" in the panel.
    /// </summary>
    void UpdateMembership(IEnumerable<IGroupable> items)
    {
        HashSet<Group> touched = new HashSet<Group>();
        foreach (IGroupable it in items)
        {
            Group? old = FindGroup(it.GroupId);
            Group? g = GroupAt(it.X + it.W / 2, it.Y + it.H / 2) ?? old;
            it.GroupId = g?.Id;
            if (g is not null) touched.Add(g);
            if (old is not null && old != g) touched.Add(old);
        }
        foreach (Group g in touched) GrowGroup(g);
    }

    /// <summary>Expands a group (never shrinks it) so all members fit inside with padding.</summary>
    void GrowGroup(Group g)
    {
        List<IBox> members = Members(g).Cast<IBox>().ToList();
        if (members.Count == 0) return;
        (double x, double y, double w, double h) = Geometry.GroupBounds(members);
        double right = Math.Max(g.X + g.W, x + w), bottom = Math.Max(g.Y + g.H, y + h);
        g.X = Math.Min(g.X, x); g.Y = Math.Min(g.Y, y);
        g.W = right - g.X; g.H = bottom - g.Y;
    }

    /// <summary>Shrink-wraps a group around its members.</summary>
    void FitGroup(Group g)
    {
        List<IBox> members = Members(g).Cast<IBox>().ToList();
        if (members.Count == 0) return;
        (g.X, g.Y, g.W, g.H) = Geometry.GroupBounds(members);
    }

    string NextGroupColour()
    {
        HashSet<string> used = D.Groups.Select(g => g.Color).ToHashSet();
        return Palette.All.Keys.FirstOrDefault(k => !used.Contains(k)) ?? Palette.All.Keys.ElementAt(D.Groups.Count % Palette.All.Count);
    }

    void GroupSelection()
    {
        List<IGroupable> items = Sel.Select(FindBox).OfType<IGroupable>().ToList();
        if (items.Count == 0) { ShowToast("Select shapes or text boxes to group"); return; }
        Change();
        Group g = new Group { Title = "Group", Color = NextGroupColour() };
        (g.X, g.Y, g.W, g.H) = Geometry.GroupBounds(items);
        D.Groups.Add(g);
        foreach (IGroupable it in items) it.GroupId = g.Id;
        SelectOnly(g.Id);
        BeginEdit(g.Id, checkpoint: false);
    }

    void Ungroup(Group g)
    {
        Change();
        foreach (IGroupable m in Members(g)) m.GroupId = null;
        D.Edges.RemoveAll(e => e.From == g.Id || e.To == g.Id);
        D.Groups.Remove(g);
        ClearSelection();
    }

    void RemoveFromGroup(IGroupable item)
    {
        if (FindGroup(item.GroupId) is not { } g) return;
        Change();
        item.GroupId = null;
        // Move it just outside the group so it doesn't look like it's still inside.
        item.X = SnapV(g.X + g.W + 40);
        FitGroup(g);
    }

    /// <summary>Moves a box; a group carries its members with it.</summary>
    void MoveBox(IBox b, double x, double y)
    {
        double dx = x - b.X, dy = y - b.Y;
        b.X = x; b.Y = y;
        if (b is Group g)
            foreach (IGroupable m in Members(g)) { m.X += dx; m.Y += dy; }
    }

    // ───────────── Alignment ─────────────

    /// <summary>
    /// Aligns/arranges items. Inside a group the group's interior is the reference; otherwise the selection bounds.
    /// Left/centre/right/top/middle/bottom line up the shape outlines only — a label drawn under a circle or outcome
    /// is ignored, so a circle and a box aligned to the bottom share the same bottom edge.
    /// Distribute and column still leave room for those labels so they don't run into the next item.
    /// </summary>
    void Align(AlignOp op, List<IBox> items, Group? within)
    {
        if (items.Count == 0 || (within is null && items.Count < 2)) return;
        Change();
        double L, T, R, B;
        if (within is not null)
        {
            L = within.X + Group.Pad; R = within.X + within.W - Group.Pad;
            T = within.Y + Group.Header + Group.Pad;
            // Leave room inside the group for the tallest label hanging below an outline.
            B = within.Y + within.H - Group.Pad - items.Max(Geometry.ExtraBelow);
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
            foreach (Group g in D.Groups.Where(g => items.OfType<IGroupable>().Any(i => i.GroupId == g.Id)).ToList())
                GrowGroup(g);
        }
    }

    void AlignSelection(AlignOp op) => Align(op, Sel.Select(FindBox).Where(b => b is not null).Cast<IBox>().ToList(), null);

    void AlignInGroup(Group g, AlignOp op) => Align(op, Members(g).Cast<IBox>().ToList(), g);

    // ───────────── Creating items ─────────────

    static Dictionary<string, string> DefaultProps(string type) => type switch
    {
        NodeTypes.Check => new() { ["ruleId"] = "", ["ifcEntity"] = "", ["property"] = "", ["operator"] = "", ["value"] = "" },
        NodeTypes.Decision => new() { ["condition"] = "" },
        NodeTypes.Data => new() { ["source"] = "" },
        NodeTypes.Status => new() { ["bcfStatus"] = "" },
        NodeTypes.Action => new() { ["owner"] = "" },
        _ => new(),
    };

    /// <summary>Palette keys are a node type, or "outcome:&lt;variant&gt;" for an outcome.</summary>
    static (string Type, string? Outcome) ParsePaletteKey(string key) =>
        key.StartsWith(NodeTypes.Outcome + ":") ? (NodeTypes.Outcome, key[(NodeTypes.Outcome.Length + 1)..]) : (key, null);

    Node CreateNode(string key, double cx, double cy)
    {
        (string type, string? outcome) = ParsePaletteKey(key);
        NodeTypeInfo t = NodeTypes.Get(type);
        bool isOutcome = t.Key == NodeTypes.Outcome;
        Node n = new Node
        {
            Type = t.Key,
            Outcome = isOutcome ? Outcomes.Get(outcome).Key : null,
            Filled = isOutcome,
            Label = t.Key == NodeTypes.Circle ? "Label" : isOutcome ? Outcomes.Get(outcome).Display : t.Display,
            Icon = t.Key == NodeTypes.Circle ? "layers" : null,
            W = t.W,
            H = t.H,
            X = SnapV(cx) - t.W / 2,
            Y = SnapV(cy) - t.H / 2,
            Properties = DefaultProps(t.Key),
        };
        D.Nodes.Add(n);
        return n;
    }

    bool AddEdge(string from, string to, string label = "")
    {
        if (from == to) return false;
        if (D.Edges.Any(e => (e.From == from && e.To == to))) return false;
        IBox? a = FindBox(from); IBox? b = FindBox(to);
        if (a is null || b is null) return false;
        // Two comments pinned to each other is meaningless; any other pairing (text ↔ shape, text ↔ text) is fine.
        bool aNote = a is TextBox { IsComment: true }, bNote = b is TextBox { IsComment: true };
        if (aNote && bNote) return false;
        // A group connecting to its own member is meaningless.
        if (a is Group ga && (b as IGroupable)?.GroupId == ga.Id) return false;
        if (b is Group gb && (a as IGroupable)?.GroupId == gb.Id) return false;
        Edge edge = new Edge { From = from, To = to, Label = label };
        edge.CopyLookFrom(D.DefaultLook);
        edge.Style = D.DefaultConnector;
        if (aNote || bNote) TextBox.ApplyPinLook(edge);
        D.Edges.Add(edge);
        return true;
    }

    /// <summary>The item new shapes are auto-joined to: the most recently selected shape, group or (non-comment) text box.</summary>
    IBox? JoinSource()
    {
        if (Sel.Count == 0) return null;
        IBox? b = FindBox(Sel[^1]);
        return b is Node or Group or TextBox { IsComment: false } ? b : null;
    }

    void AddNodeAt(string type, double cx, double cy, bool join, string? groupId = null)
    {
        Change();
        IBox? src = join ? JoinSource() : null;
        Node n = CreateNode(type, cx, cy);
        if (src is not null) AddEdge(src.Id, n.Id);
        if (groupId is not null && FindGroup(groupId) is { } g) { n.GroupId = g.Id; GrowGroup(g); }
        else UpdateMembership(new[] { n });
        SelectOnly(n.Id);
    }

    /// <summary>Palette click or number key: place the next step below the selection, joined to it.</summary>
    void QuickAdd(string type)
    {
        IBox? src = JoinSource();
        NodeTypeInfo t = NodeTypes.Get(ParsePaletteKey(type).Type);
        double cx, cy;
        if (src is not null)
        {
            cx = src.X + src.W / 2;
            cy = src.Y + src.H + Geometry.ExtraBelow(src) + 70 + t.H / 2;
            // Step sideways until the spot is free (e.g. second branch of a decision).
            int guard = 0;
            while (D.Nodes.Any(o => Geometry.Intersects(o, cx - t.W / 2 - 10, cy - t.H / 2 - 10, cx + t.W / 2 + 10, cy + t.H / 2 + 10)) && guard++ < 20)
                cx += t.W + 40;
        }
        else
        {
            (cx, cy) = ViewCentre();
            while (D.Nodes.Any(o => Geometry.Contains(o, cx, cy))) cy += 100;
        }
        // A step added from a shape inside a group stays in that group.
        AddNodeAt(type, cx, cy, join: src is not null, groupId: (src as IGroupable)?.GroupId);
    }

    /// <summary>Adds a text box (or a comment) with its top-left at (x, y), optionally joined/pinned to an item.</summary>
    void AddTextAt(double x, double y, string? joinTo, bool comment)
    {
        Change();
        TextBox t = comment ? TextBox.NewComment() : new TextBox();
        t.X = SnapV(x); t.Y = SnapV(y);
        D.TextBoxes.Add(t);
        // Comments point at what they annotate; plain text continues the flow from the selection.
        if (joinTo is not null) { if (comment) AddEdge(t.Id, joinTo); else AddEdge(joinTo, t.Id); }
        UpdateMembership(new[] { t });
        SelectOnly(t.Id);
        BeginEdit(t.Id, checkpoint: false);
    }

    void AddCommentQuick()
    {
        IBox? src = Sel.Count > 0 ? FindBox(Sel[^1]) : null;
        if (src is TextBox { IsComment: true }) src = null;
        if (src is not null) AddTextAt(src.X + src.W + 50, src.Y - 30, src.Id, comment: true);
        else { (double cx, double cy) = ViewCentre(); AddTextAt(cx - 100, cy - 55, null, comment: true); }
    }

    void AddTextQuick()
    {
        IBox? src = JoinSource();
        TextBox t = new TextBox();
        if (src is not null && AutoConnect)
            AddTextAt(src.X + src.W / 2 - t.W / 2, src.Y + src.H + Geometry.ExtraBelow(src) + 60, src.Id, comment: false);
        else { (double cx, double cy) = ViewCentre(); AddTextAt(cx - t.W / 2, cy - t.H / 2, null, comment: false); }
    }

    void AddGroupAt(double cx, double cy)
    {
        Change();
        Group g = new Group { Color = NextGroupColour() };
        g.X = SnapV(cx - g.W / 2); g.Y = SnapV(cy - g.H / 2);
        D.Groups.Add(g);
        SelectOnly(g.Id);
    }

    void AddGroupQuick()
    {
        if (Sel.Any(id => FindBox(id) is IGroupable)) { GroupSelection(); return; }
        (double cx, double cy) = ViewCentre();
        AddGroupAt(cx, cy);
    }

    // ───────────── Palette drag and drop ─────────────

    void OnDrop(DragEventArgs e)
    {
        if (paletteDrag is null) return;
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        switch (paletteDrag)
        {
            case "comment":
                AddTextAt(x - 100, y - 55, (HitTest(x, y) as Node)?.Id, comment: true);
                break;
            case "text":
                AddTextAt(x - 100, y - 30, null, comment: false);
                break;
            case "group":
                AddGroupAt(x, y);
                break;
            default:
                AddNodeAt(paletteDrag, x, y, join: AutoConnect);
                break;
        }
        paletteDrag = null;
    }

    // ───────────── Selection ─────────────

    void SelectOnly(string id) { Sel.Clear(); Sel.Add(id); SelEdge = null; }

    void ClearSelection() { Sel.Clear(); SelEdge = null; }

    // ───────────── Pointer handling ─────────────

    void ItemDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0) { StartPan(e); return; }
        if (EditId is not null && EditId != id) EndEdit();
        SelEdge = null;
        if (e.ShiftKey || e.CtrlKey)
        {
            if (Sel.Contains(id)) { Sel.Remove(id); return; }
            Sel.Add(id);
        }
        else if (!Sel.Contains(id))
        {
            SelectOnly(id);
        }
        (startX, startY) = ToWorld(e.ClientX, e.ClientY);
        moveOrigins = WithMembers(Sel).ToDictionary(b => b.Id, b => (b.X, b.Y));
        gestureSnapshot = Snapshot();
        moved = false;
        mode = Mode.Move;
    }

    /// <summary>Pressing on a group's body: click selects the group, drag draws a selection box inside it.</summary>
    void GroupBodyDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0) { StartPan(e); return; }
        if (e.ShiftKey || e.CtrlKey) { ItemDown(e, id); return; }
        EndEdit();
        ClearSelection();
        pendingGroupSelect = id;
        (startX, startY) = ToWorld(e.ClientX, e.ClientY);
        (curX, curY) = (startX, startY);
        mode = Mode.Marquee;
    }

    void PortDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0) { StartPan(e); return; }
        EndEdit();
        SelectOnly(id);
        connectFrom = id;
        (startX, startY) = ToWorld(e.ClientX, e.ClientY);
        (curX, curY) = (startX, startY);
        mode = Mode.Connect;
    }

    void ResizeDown(PointerEventArgs e, string id)
    {
        resizeId = id;
        gestureSnapshot = Snapshot();
        (startX, startY) = ToWorld(e.ClientX, e.ClientY);
        IBox b = FindBox(id)!;
        moveOrigins = new() { [id] = (b.W, b.H) };
        moved = false;
        mode = Mode.Resize;
    }

    void EdgeDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0) { StartPan(e); return; }
        if (EditId is not null && EditId != id) EndEdit();
        Sel.Clear();
        SelEdge = id;
    }

    /// <summary>Pressing a ● end of the selected connector: drag it to another side of its shape.</summary>
    void EdgeEndDown(PointerEventArgs e, string id, bool fromEnd)
    {
        if (e.Button != 0) { StartPan(e); return; }
        edgeDragId = id;
        edgeDragFrom = fromEnd;
        gestureSnapshot = Snapshot();
        (startX, startY) = ToWorld(e.ClientX, e.ClientY);
        (curX, curY) = (startX, startY);
        moved = false;
        mode = Mode.EdgeEnd;
    }

    /// <summary>Pressing the ■ on an elbow connector's middle segment: slide that segment sideways.</summary>
    void BendDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0) { StartPan(e); return; }
        Edge? edge = D.Edges.FirstOrDefault(x => x.Id == id);
        if (edge is null || FindBox(edge.From) is not { } a || FindBox(edge.To) is not { } b) return;
        Geometry.EdgeRoute r = Geometry.Route(a, b, edge);
        if (r.BendIndex < 0) return;
        edgeDragId = id;
        bendVertical = r.BendIsVertical;
        bendCoord0 = bendVertical ? r.Points[r.BendIndex].X : r.Points[r.BendIndex].Y;
        bend0 = edge.Bend;
        gestureSnapshot = Snapshot();
        (startX, startY) = ToWorld(e.ClientX, e.ClientY);
        (curX, curY) = (startX, startY);
        moved = false;
        mode = Mode.Bend;
    }

    /// <summary>
    /// The first time a connector is adjusted by hand, fix both ends to the sides they use now,
    /// so the change you make doesn't make the other end jump, and moving shapes later keeps your choice.
    /// </summary>
    void PinSides(Edge edge)
    {
        if (edge.Style == EdgeStyles.Straight) return;
        if (FindBox(edge.From) is not { } a || FindBox(edge.To) is not { } b) return;
        Geometry.EdgeRoute r = Geometry.Route(a, b, edge);
        edge.FromSide ??= r.FromSide;
        edge.ToSide ??= r.ToSide;
    }

    void DragEdgeEnd()
    {
        Edge? edge = D.Edges.FirstOrDefault(x => x.Id == edgeDragId);
        if (edge is null) return;
        IBox? box = FindBox(edgeDragFrom ? edge.From : edge.To);
        if (box is null) return;
        // Nearest side middle to the pointer.
        string side = Sides.All.MinBy(s =>
        {
            (double px, double py) = Geometry.SidePoint(box, s);
            return (px - curX) * (px - curX) + (py - curY) * (py - curY);
        })!;
        Geometry.EdgeRoute route = Geometry.Route(FindBox(edge.From)!, FindBox(edge.To)!, edge);
        string? current = edgeDragFrom ? route.FromSide : route.ToSide;
        if (side == current) return;
        if (!moved && Math.Abs(curX - startX) + Math.Abs(curY - startY) < 4) return;
        if (!moved) PinSides(edge);
        moved = true;
        if (edgeDragFrom) edge.FromSide = side; else edge.ToSide = side;
        edge.Bend = 0; // a new side means a new shape of route — start its bend from the middle again
    }

    void DragBend(double dx, double dy)
    {
        Edge? edge = D.Edges.FirstOrDefault(x => x.Id == edgeDragId);
        if (edge is null) return;
        double d = bendVertical ? dx : dy;
        if (!moved && Math.Abs(d) < 4) return;
        if (!moved) PinSides(edge);
        moved = true;
        // Snap the segment itself (not the offset) to the grid.
        edge.Bend = bend0 + SnapV(bendCoord0 + d) - bendCoord0;
    }

    void ResetRoute(Edge e)
    {
        if (!e.IsManual) return;
        Change();
        e.ResetRoute();
    }

    void ResetBend(string id)
    {
        Edge? edge = D.Edges.FirstOrDefault(x => x.Id == id);
        if (edge is null || edge.Bend == 0) return;
        Change();
        edge.Bend = 0;
    }

    void StartPan(PointerEventArgs e)
    {
        lastClientX = e.ClientX; lastClientY = e.ClientY;
        mode = Mode.Pan;
    }

    void BgDown(PointerEventArgs e)
    {
        EndEdit();
        if (e.Button != 0) { StartPan(e); return; }
        if (!e.ShiftKey) ClearSelection();
        pendingGroupSelect = null;
        (startX, startY) = ToWorld(e.ClientX, e.ClientY);
        (curX, curY) = (startX, startY);
        mode = Mode.Marquee;
    }

    void BgDoubleClick(MouseEventArgs e)
    {
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        AddNodeAt(NodeTypes.Process, x, y, join: false);
    }

    void PointerMove(PointerEventArgs e)
    {
        switch (mode)
        {
            case Mode.None:
                skipRender = true;
                return;
            case Mode.Pan:
                PanX += e.ClientX - lastClientX;
                PanY += e.ClientY - lastClientY;
                lastClientX = e.ClientX; lastClientY = e.ClientY;
                return;
        }
        (curX, curY) = ToWorld(e.ClientX, e.ClientY);
        double dx = curX - startX, dy = curY - startY;
        switch (mode)
        {
            case Mode.Move:
                if (!moved && Math.Abs(dx) + Math.Abs(dy) < 2) { skipRender = true; return; }
                moved = true;
                foreach ((string id, (double ox, double oy)) in moveOrigins)
                {
                    IBox? b = FindBox(id);
                    if (b is null) continue;
                    // Snap centres (not corners) so shapes of different sizes line up and connectors stay straight.
                    b.X = SnapV(ox + dx + b.W / 2) - b.W / 2;
                    b.Y = SnapV(oy + dy + b.H / 2) - b.H / 2;
                }
                break;
            case Mode.Resize:
                if (resizeId is not null && FindBox(resizeId) is { } r)
                {
                    (double ow, double oh) = moveOrigins[resizeId];
                    (double minW, double minH) = r switch { Group => (160.0, 100.0), TextBox => (40.0, 24.0), _ => (80.0, 40.0) };
                    r.W = Math.Max(minW, SnapV(ow + dx));
                    r.H = Math.Max(minH, SnapV(oh + dy));
                    moved = true;
                }
                break;
            case Mode.EdgeEnd:
                DragEdgeEnd();
                break;
            case Mode.Bend:
                DragBend(dx, dy);
                break;
        }
    }

    void PointerUp(PointerEventArgs e)
    {
        switch (mode)
        {
            case Mode.None:
                skipRender = true;
                return;
            case Mode.Move:
                if (moved && gestureSnapshot is not null)
                {
                    PushUndo(gestureSnapshot);
                    Dirty = true;
                    // Items dragged on their own may have entered or left a group.
                    HashSet<string> movedGroups = Sel.Where(id => FindGroup(id) is not null).ToHashSet();
                    List<IGroupable> loose = moveOrigins.Keys.Select(FindBox).OfType<IGroupable>()
                        .Where(i => i.GroupId is null || !movedGroups.Contains(i.GroupId)).ToList();
                    UpdateMembership(loose);
                }
                break;
            case Mode.Resize or Mode.EdgeEnd or Mode.Bend:
                if (moved && gestureSnapshot is not null) { PushUndo(gestureSnapshot); Dirty = true; }
                break;
            case Mode.Connect when connectFrom is not null:
            {
                IBox? target = HitTest(curX, curY, connectFrom);
                if (target is not null)
                {
                    string snap = Snapshot();
                    if (AddEdge(connectFrom, target.Id)) { PushUndo(snap); Dirty = true; }
                }
                else if (Math.Abs(curX - startX) + Math.Abs(curY - startY) > 40)
                {
                    // Dropped on empty space: create a new step there, joined to the source.
                    AddNodeAt(NodeTypes.Process, curX, curY, join: true);
                }
                break;
            }
            case Mode.Marquee:
            {
                if (Math.Abs(curX - startX) + Math.Abs(curY - startY) > 4)
                {
                    double x1 = Math.Min(startX, curX), y1 = Math.Min(startY, curY), x2 = Math.Max(startX, curX), y2 = Math.Max(startY, curY);
                    IOrderedEnumerable<IBox> hits = D.Nodes.Cast<IBox>().Concat(D.TextBoxes)
                        .Where(b => Geometry.Intersects(b, x1, y1, x2, y2))
                        // Groups only when fully enclosed, so dragging inside a group selects its contents.
                        .Concat(D.Groups.Where(g => g.X >= x1 && g.Y >= y1 && g.X + g.W <= x2 && g.Y + g.H <= y2))
                        .OrderBy(b => b.Y).ThenBy(b => b.X);
                    foreach (IBox b in hits)
                        if (!Sel.Contains(b.Id)) Sel.Add(b.Id);
                }
                else if (pendingGroupSelect is not null)
                {
                    SelectOnly(pendingGroupSelect);
                }
                break;
            }
        }
        mode = Mode.None;
        connectFrom = null;
        resizeId = null;
        edgeDragId = null;
        pendingGroupSelect = null;
        gestureSnapshot = null;
    }

    // ───────────── Commands ─────────────

    void JoinSelected()
    {
        if (Sel.Count < 2) { ShowToast("Select two or more items to join"); return; }
        string snap = Snapshot();
        int added = 0;
        for (int i = 0; i < Sel.Count - 1; i++)
            if (AddEdge(Sel[i], Sel[i + 1])) added++;
        if (added > 0) { PushUndo(snap); Dirty = true; }
        ShowToast(added == 0 ? "Already joined" : $"Joined {added} connector{(added == 1 ? "" : "s")}");
    }

    void DeleteSelection()
    {
        if (Sel.Count == 0 && SelEdge is null) return;
        Change();
        if (SelEdge is not null) D.Edges.RemoveAll(e => e.Id == SelEdge);
        // Deleting a group deletes its contents too (Ungroup keeps them).
        HashSet<string> ids = WithMembers(Sel).Select(b => b.Id).ToHashSet();
        D.Nodes.RemoveAll(n => ids.Contains(n.Id));
        D.TextBoxes.RemoveAll(c => ids.Contains(c.Id));
        D.Groups.RemoveAll(g => ids.Contains(g.Id));
        D.Edges.RemoveAll(e => ids.Contains(e.From) || ids.Contains(e.To));
        ClearSelection();
        EditId = null;
    }

    void Duplicate()
    {
        if (Sel.Count == 0) return;
        Change();
        Dictionary<string, string> map = new Dictionary<string, string>();
        List<IBox> items = WithMembers(Sel);
        // Groups first so members can be re-pointed at the copies.
        foreach (Group g in items.OfType<Group>())
        {
            Group gg = new Group { Title = g.Title, Color = g.Color, X = g.X + 30, Y = g.Y + 30, W = g.W, H = g.H };
            D.Groups.Add(gg); map[g.Id] = gg.Id;
        }
        string? NewGroup(string? old) => old is not null && map.TryGetValue(old, out string? ng) ? ng : old;
        foreach (IBox b in items)
        {
            switch (b)
            {
                case Node n:
                    Node nn = new Node
                    {
                        Type = n.Type, Label = n.Label, X = n.X + 30, Y = n.Y + 30, W = n.W, H = n.H,
                        Icon = n.Icon, Color = n.Color, Filled = n.Filled, Outcome = n.Outcome, GroupId = NewGroup(n.GroupId),
                        Properties = new(n.Properties),
                    };
                    D.Nodes.Add(nn); map[n.Id] = nn.Id; break;
                case TextBox c:
                    TextBox cc = c.Copy();
                    cc.X += 30; cc.Y += 30; cc.GroupId = NewGroup(c.GroupId);
                    D.TextBoxes.Add(cc); map[c.Id] = cc.Id; break;
            }
        }
        foreach (Edge e in D.Edges.ToList())
            if (map.TryGetValue(e.From, out string? f) && map.TryGetValue(e.To, out string? t))
            {
                Edge copy = new Edge { From = f, To = t, Label = e.Label, LabelPos = e.LabelPos };
                copy.CopyLookFrom(e);
                copy.CopyRouteFrom(e);
                D.Edges.Add(copy);
            }
        List<string> newSel = Sel.Where(map.ContainsKey).Select(id => map[id]).ToList();
        Sel.Clear();
        Sel.AddRange(newSel);
    }

    void Nudge(double dx, double dy)
    {
        if (Sel.Count == 0) return;
        Change();
        foreach (IBox b in WithMembers(Sel)) { b.X += dx; b.Y += dy; }
        foreach (Group g in D.Groups.Where(g => !Sel.Contains(g.Id) && Members(g).Any(m => Sel.Contains(m.Id))).ToList())
            GrowGroup(g);
    }

    /// <summary>Small inline sample of a line look for the panel buttons.</summary>
    static MarkupString LinePreview(string weight, string pattern, string arrows)
    {
        double w = LineLook.Width(weight);
        string? dash = LineLook.Dash(pattern, w);
        int x1 = arrows == LineLook.ArrowBoth ? 7 : 2;
        int x2 = arrows == LineLook.ArrowNone ? 38 : 33;
        string head = "";
        if (arrows != LineLook.ArrowNone) head += "<path d=\"M32,2 L40,6 L32,10 z\" fill=\"currentColor\"/>";
        if (arrows == LineLook.ArrowBoth) head += "<path d=\"M8,2 L0,6 L8,10 z\" fill=\"currentColor\"/>";
        return new($"<svg width=\"40\" height=\"12\" viewBox=\"0 0 40 12\"><line x1=\"{x1}\" y1=\"6\" x2=\"{x2}\" y2=\"6\" stroke=\"currentColor\" stroke-width=\"{F(w)}\"{(dash is null ? "" : $" stroke-dasharray=\"{dash}\"")} stroke-linecap=\"{LineLook.Cap(pattern)}\"/>{head}</svg>");
    }

    // ───────────── Page (paper size) ─────────────

    /// <summary>Items not fully inside the page (null when there is no page).</summary>
    List<IBox> OutsidePage()
    {
        if (!D.Page.Enabled) return new();
        double W = D.Page.WidthPx, H = D.Page.HeightPx;
        return D.Nodes.Cast<IBox>().Concat(D.TextBoxes).Concat(D.Groups)
            .Where(b => b.X < -0.5 || b.Y < -0.5 || b.X + b.W > W + 0.5 || b.Y + b.H + Geometry.ExtraBelow(b) > H + 0.5)
            .ToList();
    }

    bool ContentTooBigForPage()
    {
        if (!D.Page.Enabled || Geometry.ContentBounds(D) is not { } cb) return false;
        double m = D.Page.MarginPx;
        return cb.W > D.Page.WidthPx - 2 * m + 0.5 || cb.H > D.Page.HeightPx - 2 * m + 0.5;
    }

    void SetPageSize(string? size)
    {
        if (size is null || size == D.Page.Size) return;
        Change();
        bool wasOpen = !D.Page.Enabled;
        D.Page.Size = size;
        if (D.Page.Enabled && wasOpen && OutsidePage().Count > 0) CentreOnPage(checkpoint: false);
        Fit();
        if (D.Page.Enabled && ContentTooBigForPage())
            ShowToast(D.Page.FitOnExport ? "Drawing is bigger than the page — it will be scaled down to fit on export" : "Drawing is bigger than the page");
    }

    void SetOrientation(string orientation)
    {
        if (D.Page.Size == PageSetup.Custom)
        {
            // Custom sizes: orientation just swaps the numbers.
            bool isLandscape = D.Page.CustomW > D.Page.CustomH;
            if ((orientation == "landscape") == isLandscape) return;
            Change();
            (D.Page.CustomW, D.Page.CustomH) = (D.Page.CustomH, D.Page.CustomW);
        }
        else
        {
            if (D.Page.Orientation == orientation) return;
            Change();
        }
        D.Page.Orientation = orientation;
        Fit();
    }

    bool PageIsLandscape => D.Page.Size == PageSetup.Custom ? D.Page.CustomW > D.Page.CustomH : D.Page.Landscape;

    void CentreOnPage() => CentreOnPage(checkpoint: true);

    /// <summary>Moves the whole drawing so it is centred inside the page margins.</summary>
    void CentreOnPage(bool checkpoint)
    {
        if (!D.Page.Enabled || Geometry.ContentBounds(D) is not { } cb) return;
        if (checkpoint) Change();
        double W = D.Page.WidthPx, H = D.Page.HeightPx, m = D.Page.MarginPx;
        double dx = SnapV(m + (W - 2 * m - cb.W) / 2 - cb.X);
        double dy = SnapV(m + (H - 2 * m - cb.H) / 2 - cb.Y);
        foreach (IBox b in D.Nodes.Cast<IBox>().Concat(D.TextBoxes).Concat(D.Groups)) { b.X += dx; b.Y += dy; }
        if (checkpoint && ContentTooBigForPage())
            ShowToast("Centred — but the drawing is bigger than the printable area");
    }

    void SelectOutsidePage()
    {
        Sel.Clear(); SelEdge = null;
        Sel.AddRange(OutsidePage().Select(b => b.Id));
    }

    void PageChanged() { Dirty = true; }

    // ───────────── Connector style & labels ─────────────

    void SetDefaultConnector(string style)
    {
        D.DefaultConnector = style;
        Dirty = true;
        ShowToast($"New connectors will be {style}" + (SelectedEdgeObj is null ? "" : " — use the panel to change the selected one"));
    }

    void SetEdgeStyle(Edge e, string style) { if (e.Style == style) return; Change(); e.Style = style; }
    void SetLabelOrient(Edge e, string orient) { if (e.LabelOrient == orient) return; Change(); e.LabelOrient = orient; }
    void SetLabelPos(Edge e, double pos) { Change(); e.LabelPos = pos; }

    void DragLabelPos(Edge e, object? value)
    {
        if (double.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
        {
            e.LabelPos = Math.Clamp(v, 0, 1);
            Dirty = true;
        }
    }

    void ApplyEdgeLookToAll(Edge src)
    {
        Change();
        foreach (Edge e in D.Edges) e.CopyLookFrom(src);
        D.DefaultLook.CopyLookFrom(src);
        D.DefaultConnector = src.Style;
        ShowToast($"Applied to {D.Edges.Count} connectors");
    }

    void SetEdgeLook(Edge e, Action<Edge> change) { Change(); change(e); }

    void MakeDefaultLook(Edge src)
    {
        Dirty = true;
        D.DefaultLook.CopyLookFrom(src);
        D.DefaultConnector = src.Style;
        ShowToast("New connectors will use this look");
    }

    void SetStyleBetweenSelected(string style)
    {
        HashSet<string> ids = WithMembers(Sel).Select(b => b.Id).ToHashSet();
        List<Edge> edges = D.Edges.Where(e => ids.Contains(e.From) && ids.Contains(e.To)).ToList();
        if (edges.Count == 0) { ShowToast("No connectors between the selected items"); return; }
        Change();
        foreach (Edge e in edges) e.Style = style;
        ShowToast($"{edges.Count} connector{(edges.Count == 1 ? "" : "s")} set to {style}");
    }

    void ReverseEdge(Edge e)
    {
        Change();
        (e.From, e.To) = (e.To, e.From);
        (e.FromSide, e.ToSide) = (e.ToSide, e.FromSide);
    }

    void ChangeType(Node n, string? type)
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
        foreach ((string k, string v) in DefaultProps(t.Key)) n.Properties.TryAdd(k, v);
    }

    void SetNodeColour(Node n, string? colour) { Change(); n.Color = colour; }
    void SetFilled(Node n, bool filled) { Change(); n.Filled = filled; }

    void SetOutcome(Node n, string outcome)
    {
        if (n.Outcome == outcome) return;
        Change();
        // A label still showing the old outcome's name follows the new one.
        if (n.Label == Outcomes.Get(n.Outcome).Display) n.Label = Outcomes.Get(outcome).Display;
        n.Outcome = outcome;
    }
    void SetIcon(Node n, string? icon) { Change(); n.Icon = icon; }
    void SetGroupColour(Group g, string colour) { Change(); g.Color = colour; }

    // ───────────── Text boxes ─────────────

    void SetTextLook(TextBox t, Action<TextBox> change) { Change(); change(t); }

    /// <summary>Turning the comment flag on applies the standard comment look; turning it off keeps the look.</summary>
    void SetComment(TextBox t, bool on)
    {
        if (t.IsComment == on) return;
        Change();
        if (on) t.ApplyCommentStyle(); else t.IsComment = false;
        foreach (Edge e in D.Edges.Where(e => e.From == t.Id || e.To == t.Id))
        {
            if (on) TextBox.ApplyPinLook(e);
            else { e.CopyLookFrom(D.DefaultLook); e.Style = D.DefaultConnector; }
        }
        // Comments can't be pinned to other comments.
        if (on) D.Edges.RemoveAll(e => (e.From == t.Id && FindBox(e.To) is TextBox { IsComment: true }) || (e.To == t.Id && FindBox(e.From) is TextBox { IsComment: true }));
    }

    void ResetCommentStyle(TextBox t) { Change(); t.ApplyCommentStyle(); }

    /// <summary>Sets the height so the wrapped text just fits.</summary>
    void FitTextHeight(TextBox t)
    {
        Change();
        t.H = Math.Max(24, Math.Ceiling(Geometry.Layout(t).ContentHeight / 2) * 2);
    }

    void SetFontSize(TextBox t, object? value)
    {
        if (!double.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) return;
        Change();
        t.FontSize = Math.Clamp(v, 6, 96);
    }

    void AddProp(Node n)
    {
        Change();
        int i = 1;
        while (n.Properties.ContainsKey($"prop{i}")) i++;
        n.Properties[$"prop{i}"] = "";
    }

    void RenameProp(Node n, string oldKey, string? newKey)
    {
        newKey = newKey?.Trim();
        if (string.IsNullOrEmpty(newKey) || newKey == oldKey || n.Properties.ContainsKey(newKey)) return;
        // Rebuild to keep ordering stable.
        Dictionary<string, string> rebuilt = n.Properties.ToDictionary(kv => kv.Key == oldKey ? newKey : kv.Key, kv => kv.Value);
        n.Properties = rebuilt;
        Dirty = true;
    }

    // ───────────── Inline text editing ─────────────

    void BeginEdit(string id) => BeginEdit(id, checkpoint: true);

    void BeginEdit(string id, bool checkpoint)
    {
        if (checkpoint) Checkpoint();
        mode = Mode.None;
        EditId = id;
        if (D.Edges.Any(e => e.Id == id)) { Sel.Clear(); SelEdge = id; }
        else if (!Sel.Contains(id)) SelectOnly(id);
        _ = module?.InvokeVoidAsync("focus", "edit-box");
    }

    void EndEdit()
    {
        if (EditId is null) return;
        if (D.Nodes.FirstOrDefault(n => n.Id == EditId) is { } n) n.Label = n.Label.TrimEnd('\n', '\r');
        EditId = null;
    }

    void EditKey(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey) EndEdit();
    }

    // ───────────── View ─────────────

    void ZoomAt(double factor, double px, double py)
    {
        double z = Math.Clamp(Zoom * factor, 0.05, 3);
        PanX = px - (px - PanX) * z / Zoom;
        PanY = py - (py - PanY) * z / Zoom;
        Zoom = z;
    }

    void ZoomBy(double factor) => ZoomAt(factor, canvasW / 2, canvasH / 2);

    void Fit()
    {
        double minX, minY, maxX, maxY;
        (double X, double Y, double W, double H)? cb = Geometry.ContentBounds(D);
        if (D.Page.Enabled)
        {
            // Show the whole page (plus anything hanging off it).
            minX = 0; minY = 0; maxX = D.Page.WidthPx; maxY = D.Page.HeightPx;
            if (cb is { } c) { minX = Math.Min(minX, c.X); minY = Math.Min(minY, c.Y); maxX = Math.Max(maxX, c.X + c.W); maxY = Math.Max(maxY, c.Y + c.H); }
            minY -= 24; // room for the page caption
        }
        else
        {
            if (cb is not { } c) { Zoom = 1; PanX = 40; PanY = 40; return; }
            (minX, minY, maxX, maxY) = (c.X, c.Y, c.X + c.W, c.Y + c.H);
        }
        Zoom = Math.Clamp(Math.Min((canvasW - 80) / (maxX - minX), (canvasH - 80) / (maxY - minY)), 0.05, 1.5);
        PanX = canvasW / 2 - (minX + maxX) / 2 * Zoom;
        PanY = canvasH / 2 - (minY + maxY) / 2 * Zoom;
    }

    [JSInvokable]
    public void OnCanvasRect(double left, double top, double width, double height)
    {
        canvasLeft = left; canvasTop = top; canvasW = width; canvasH = height;
    }

    [JSInvokable]
    public void OnWheel(double dx, double dy, bool ctrl, bool shift, double px, double py)
    {
        if (ctrl) ZoomAt(Math.Exp(-dy * 0.0015), px, py);
        else if (shift) PanX -= dy;
        else { PanX -= dx; PanY -= dy; }
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnKey(string key, bool ctrl, bool shift)
    {
        if (dialog != Dialog.None)
        {
            if (key == "Escape") CloseDialog();
            StateHasChanged();
            return;
        }
        string k = key.Length == 1 ? key.ToLowerInvariant() : key;
        if (ctrl)
        {
            switch (k)
            {
                case "s": if (shift) ShowSaveAs(); else await Save(); break;
                case "z": if (shift) Redo(); else Undo(); break;
                case "y": Redo(); break;
                case "a":
                    Sel.Clear();
                    Sel.AddRange(D.Nodes.Select(n => n.Id)); Sel.AddRange(D.TextBoxes.Select(c => c.Id)); Sel.AddRange(D.Groups.Select(g => g.Id));
                    SelEdge = null; break;
                case "d": Duplicate(); break;
                case "g":
                    if (shift) { if (SelectedGroup is { } g) Ungroup(g); }
                    else GroupSelection();
                    break;
                default: return;
            }
            StateHasChanged();
            return;
        }
        int step = shift ? 1 : 10;
        switch (k)
        {
            case "Delete" or "Backspace": DeleteSelection(); break;
            case "Escape": EditId = null; mode = Mode.None; ClearSelection(); break;
            case "Enter":
                if (Sel.Count == 1) BeginEdit(Sel[0]);
                else if (SelEdge is not null) BeginEdit(SelEdge);
                break;
            case "ArrowUp": Nudge(0, -step); break;
            case "ArrowDown": Nudge(0, step); break;
            case "ArrowLeft": Nudge(-step, 0); break;
            case "ArrowRight": Nudge(step, 0); break;
            case "j": JoinSelected(); break;
            case "l":
                if (SelectedEdgeObj is { } le) SetEdgeStyle(le, le.Style == EdgeStyles.Straight ? EdgeStyles.Elbow : EdgeStyles.Straight);
                else return;
                break;
            case "c": AddCommentQuick(); break;
            case "t": AddTextQuick(); break;
            case "g": AddGroupQuick(); break;
            case "f": Fit(); break;
            case "+" or "=": ZoomBy(1.2); break;
            case "-": ZoomBy(1 / 1.2); break;
            case "0": Zoom = 1; break;
            default:
                if (k.Length == 1 && k[0] >= '1' && k[0] <= '9' && k[0] - '1' < NodeTypes.All.Count)
                    QuickAdd(NodeTypes.All[k[0] - '1'].Key);
                else return;
                break;
        }
        StateHasChanged();
    }

    // ───────────── Files ─────────────

    void Reset(bool keepTrail = false)
    {
        if (!keepTrail) trail.Clear();
        D = new Diagram();
        hasFile = false;
        Dirty = false;
        undo.Clear(); redo.Clear();
        ClearSelection();
        EditId = null;
        Zoom = 1; PanX = 40; PanY = 40;
    }

    void NewDiagram()
    {
        if (Dirty) dialog = Dialog.ConfirmNew;
        else Reset();
    }

    void ShowOpen()
    {
        confirmDelete = null;
        openFiles = Store.List();
        dialog = Dialog.Open;
    }

    void ShowSaveAs()
    {
        saveAsName = D.Name;
        dialog = Dialog.SaveAs;
        _ = module?.InvokeVoidAsync("focus", "saveas-name");
    }

    void CloseDialog() { dialog = Dialog.None; confirmDelete = null; }

    /// <summary>Open from the Open dialog: a fresh start, so the linked-diagram trail is cleared.</summary>
    async Task Open(string name)
    {
        if (await Load(name)) trail.Clear();
    }

    async Task<bool> Load(string name)
    {
        try
        {
            Diagram loaded = await Store.LoadAsync(name);
            Reset(keepTrail: true);
            D = loaded;
            hasFile = true;
            CloseDialog();
            Fit();
            ShowToast($"Opened {name}");
            return true;
        }
        catch (Exception ex)
        {
            ShowToast($"Could not open {name}: {ex.Message}");
            return false;
        }
    }

    // ───────────── Linked diagrams ─────────────

    /// <summary>Diagrams we drilled down from via link badges, outermost first. Shown as a breadcrumb with Back.</summary>
    readonly List<string> trail = new();

    /// <summary>Where we're going once the unsaved-changes question is answered. KeepTrail = trail entries to keep (-1 = push current).</summary>
    (string Target, int KeepTrail)? pendingNav;

    void SetNodeLink(Node n, string? link) { Change(); n.Link = link; }

    void SetGroupLink(Group g, string? link) { Change(); g.Link = link; }

    void FollowLink(string? link)
    {
        if (string.IsNullOrWhiteSpace(link)) return;
        if (hasFile && link == D.Name) { ShowToast("That link points to this diagram"); return; }
        if (!Store.Exists(link)) { ShowToast($"Linked diagram “{link}” not found in {Store.Folder}"); return; }
        Navigate(link, -1);
    }

    void GoBack() { if (trail.Count > 0) GoUpTo(trail.Count - 1); }

    /// <summary>Jump back to trail[level], dropping it and everything below it from the trail.</summary>
    void GoUpTo(int level)
    {
        if (level < 0 || level >= trail.Count) return;
        Navigate(trail[level], level);
    }

    void Navigate(string target, int keepTrail)
    {
        EndEdit();
        if (Dirty)
        {
            pendingNav = (target, keepTrail);
            dialog = Dialog.ConfirmLeave;
            return;
        }
        _ = DoNavigate(target, keepTrail);
    }

    async Task DoNavigate(string target, int keepTrail)
    {
        string? from = hasFile ? D.Name : null;
        List<string> before = trail.ToList();
        if (!await Load(target)) return;
        trail.Clear();
        if (keepTrail >= 0) trail.AddRange(before.Take(keepTrail));
        else
        {
            trail.AddRange(before);
            if (from is not null) trail.Add(from);
        }
        StateHasChanged();
    }

    async Task ConfirmLeave(bool save)
    {
        if (pendingNav is not { } nav) { CloseDialog(); return; }
        if (save)
        {
            await Save();
            if (Dirty) return; // save failed: stay put, the toast says why
        }
        pendingNav = null;
        CloseDialog();
        Dirty = false;
        await DoNavigate(nav.Target, nav.KeepTrail);
    }

    void CancelLeave() { pendingNav = null; CloseDialog(); }

    void DeleteFile(string name)
    {
        Store.Delete(name);
        openFiles = Store.List();
        if (hasFile && D.Name == name) { hasFile = false; Dirty = true; }
        confirmDelete = null;
        ShowToast($"Deleted {name}");
    }

    async Task Save()
    {
        EndEdit();
        if (!hasFile) { ShowSaveAs(); return; }
        try
        {
            string path = await Store.SaveAsync(D);
            Dirty = false;
            ShowToast($"Saved {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            ShowToast($"Save failed: {ex.Message}");
        }
    }

    async Task ConfirmSaveAs()
    {
        D.Name = DiagramStore.SafeName(saveAsName);
        hasFile = true;
        CloseDialog();
        await Save();
    }

    async Task SaveAsKey(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await ConfirmSaveAs();
    }

    async Task ExportSvg()
    {
        string svg = Exporters.ToSvg(D);
        string path = await Store.SaveTextAsync(D.Name, ".svg", svg);
        ShowToast($"Exported {Path.GetFileName(path)}");
    }

    /// <summary>
    /// PDF next to the saved diagram. Whole export: the page when a size is set, otherwise the drawing extents.
    /// Current view: what is on screen (fitted to the page when a size is set), saved as "name.view.pdf".
    /// </summary>
    string PdfLabel() => D.Page.Enabled
        ? $"PDF — page ({D.Page.Mm.W:0.#} × {D.Page.Mm.H:0.#} mm)"
        : "PDF — drawing extents";

    string PdfTip(bool currentView) => (currentView, D.Page.Enabled) switch
    {
        (true, true) => "What is on screen now, fitted inside the page margins",
        (true, false) => "Exactly what is on screen now",
        (false, true) => "The whole page at the selected paper size",
        _ => "The whole drawing, zoomed to its extents",
    };

    async Task ExportPdf(bool currentView)
    {
        try
        {
            ExportView? view = currentView
                ? new ExportView(-PanX / Zoom, -PanY / Zoom, canvasW / Zoom, canvasH / Zoom, canvasW, canvasH)
                : null;
            byte[] pdf = PdfExporter.FromSvg(Exporters.ToSvg(D, view), D.Name);
            string path = await Store.SaveBytesAsync(D.Name, currentView ? ".view.pdf" : ".pdf", pdf);
            ShowToast($"Exported {Path.GetFileName(path)}");
        }
        catch (Exception ex)
        {
            ShowToast($"PDF export failed: {ex.Message}");
        }
    }

    async Task ExportMermaid()
    {
        string path = await Store.SaveTextAsync(D.Name, ".mmd", Exporters.ToMermaid(D));
        ShowToast($"Exported {Path.GetFileName(path)}");
    }

    void ShowToast(string message)
    {
        toast = message;
        toastCts?.Cancel();
        CancellationTokenSource cts = toastCts = new CancellationTokenSource();
        _ = Task.Delay(3500, cts.Token).ContinueWith(t =>
        {
            if (t.IsCanceled) return;
            toast = null;
            _ = InvokeAsync(StateHasChanged);
        }, TaskScheduler.Default);
    }

    // ───────────── Render helpers (read by Designer.razor) ─────────────
    // The markup only loops and reads values; anything that needs working out lives here.

    public record PaletteShape(NodeTypeInfo Type, int Number);
    public record Crumb(int Level, string Name);
    public record GroupView(Group G, Palette.Swatch Swatch, bool Selected, string HeaderPath);
    public record TextBoxView(TextBox T, TextStyle Style, bool Selected, Geometry.TextBoxLayout Layout, string Outline);
    public record EdgeView(Edge E, bool Selected, Geometry.EdgeGeom Geo, double Width, string Colour, string Marker);
    public record EdgeLabelView(Edge E, bool Selected, bool Editing, Geometry.EdgeGeom Geo, double Width);
    public record NodeView(Node N, NodeTypeInfo Type, NodeStyle Style, bool Selected, Geometry.LabelLayout Layout, double Below);
    public record ConnectPreview(string Path, IBox? Target);
    public record SideTarget(double X, double Y, bool Current);
    public record EdgeHandleView(string EdgeId, List<SideTarget> Sides, bool HasBend, double BendX, double BendY, bool BendVertical,
                                 double StartX, double StartY, double EndX, double EndY);
    public record Rect(double X, double Y, double W, double H);

    static readonly (string Key, string Name)[] HAligns = { (TextLook.Left, "Left"), (TextLook.Center, "Centre"), (TextLook.Right, "Right") };
    static readonly (string Key, string Name)[] VAligns = { (TextLook.Top, "Top"), (TextLook.Middle, "Middle"), (TextLook.Bottom, "Bottom") };
    static readonly string[] EdgeLabelChips = { "Yes", "No", "Pass", "Fail", "" };

    /// <summary>Palette shapes (outcomes have their own section), numbered for the 1–9 shortcut keys.</summary>
    static readonly List<PaletteShape> PaletteShapes = NodeTypes.All
        .Where(t => t.Key != NodeTypes.Outcome)
        .Select((t, i) => new PaletteShape(t, i + 1))
        .ToList();

    static string OutcomePaletteKey(OutcomeInfo o) => $"{NodeTypes.Outcome}:{o.Key}";

    static string OutcomePaletteTip(OutcomeInfo o) => $"{o.Hint}. Drag onto the canvas or click to add.";

    static string PaletteShapePath(string type) => Geometry.ShapePath(new Node { Type = type, W = 48, H = 28 });

    void StartTextDrag() => paletteDrag = "text";
    void StartCommentDrag() => paletteDrag = "comment";
    void StartGroupDrag() => paletteDrag = "group";

    void SetPageSize(ChangeEventArgs e) => SetPageSize(e.Value?.ToString());
    void SetPortrait() => SetOrientation("portrait");
    void SetLandscape() => SetOrientation("landscape");

    double PrintableW => Math.Max(0, D.Page.WidthPx - 2 * D.Page.MarginPx);
    double PrintableH => Math.Max(0, D.Page.HeightPx - 2 * D.Page.MarginPx);
    string PageCaptionStyle => $"font-size:{F(12 / Zoom)}px";

    double BendHandleSize => 12 / Zoom;

    static string NodeIconTransform(Geometry.LabelLayout lay) =>
        $"translate({F(lay.IconX)},{F(lay.IconY)}) scale({F(lay.IconSize / 24)})";

    static MarkupString NodeIconMarkup(Node n) => (MarkupString)Icons.Get(Geometry.IconOf(n));

    static string EdgeLabelTransform(Geometry.EdgeGeom geo) =>
        $"translate({F(geo.LabelX)},{F(geo.LabelY)}) rotate({F(geo.LabelAngle)})";

    /// <summary>A node's label lines with the tspan line offset (first line 0, then one line height each).</summary>
    static IEnumerable<(string Text, string Dy)> LabelLines(Geometry.LabelLayout lay) =>
        lay.Lines.Select((line, i) => (line, i == 0 ? "0" : F(Geometry.LineHeight)));

    static string NodePanelTitle(Node n) =>
        n.Type == NodeTypes.Outcome ? $"Outcome · {Outcomes.Get(n.Outcome).Display}" : NodeTypes.Get(n.Type).Display;

    static IEnumerable<double> FontSizeChoices(TextBox t) => TextLook.Sizes.Append(t.FontSize).Distinct().OrderBy(v => v);

    string DiagramSummary =>
        $"{D.Nodes.Count} shapes · {D.Edges.Count} connectors · {D.TextBoxes.Count(t => !t.IsComment)} text boxes · " +
        $"{D.TextBoxes.Count(t => t.IsComment)} comments · {D.Groups.Count} groups";

    bool SaveAsWillOverwrite => Store.Exists(saveAsName) && DiagramStore.SafeName(saveAsName) != (hasFile ? D.Name : null);

    string SaveAsPath => Path.Combine(Store.Folder, DiagramStore.SafeName(saveAsName) + DiagramStore.Extension);

    IEnumerable<Crumb> Crumbs => trail.Select((name, i) => new Crumb(i, name));

    string GridTransform => $"translate({F(PanX)},{F(PanY)}) scale({F(Zoom)})";

    MarkupString ArrowMarkerDefs => (MarkupString)string.Concat(D.Edges.Append(D.DefaultLook)
        .Select(x => (Colour: LineLook.Colour(x.Color), x.Weight)).Distinct()
        .Select(m => LineLook.MarkerDef(m.Colour, m.Weight)));

    IEnumerable<GroupView> GroupViews() =>
        D.Groups.Select(g => new GroupView(g, Palette.Get(g.Color), Sel.Contains(g.Id), GroupHeaderPath(g)));

    static string GroupHeaderPath(Group g) =>
        $"M{F(g.X)},{F(g.Y + Group.Header)} V{F(g.Y + 10)} Q{F(g.X)},{F(g.Y)} {F(g.X + 10)},{F(g.Y)} H{F(g.X + g.W - 10)} Q{F(g.X + g.W)},{F(g.Y)} {F(g.X + g.W)},{F(g.Y + 10)} V{F(g.Y + Group.Header)} Z";

    IEnumerable<TextBoxView> TextBoxViews() =>
        D.TextBoxes.Select(t =>
        {
            TextStyle ts = TextStyle.For(t);
            return new TextBoxView(t, ts, Sel.Contains(t.Id), Geometry.Layout(t), Geometry.TextBoxPath(t, ts.Radius));
        });

    IEnumerable<EdgeView> EdgeViews()
    {
        foreach (Edge e in D.Edges)
        {
            if (FindBox(e.From) is not { } a || FindBox(e.To) is not { } b) continue;
            double lw = LineLook.Width(e.Weight);
            string lc = LineLook.Colour(e.Color);
            yield return new EdgeView(e, SelEdge == e.Id, Geometry.EdgeGeometry(a, b, e), lw, lc, $"url(#{LineLook.MarkerId(lc, e.Weight)})");
        }
    }

    /// <summary>Connector labels that need drawing (or editing), in their own layer above the shapes.</summary>
    IEnumerable<EdgeLabelView> EdgeLabelViews()
    {
        foreach (Edge e in D.Edges)
        {
            bool editing = EditId == e.Id;
            if (string.IsNullOrWhiteSpace(e.Label) && !editing) continue;
            if (FindBox(e.From) is not { } a || FindBox(e.To) is not { } b) continue;
            yield return new EdgeLabelView(e, SelEdge == e.Id, editing, Geometry.EdgeGeometry(a, b, e), Geometry.LabelWidth(e.Label));
        }
    }

    IEnumerable<NodeView> NodeViews() =>
        D.Nodes.Select(n =>
        {
            NodeTypeInfo t = NodeTypes.Get(n.Type);
            string label = string.IsNullOrWhiteSpace(n.Label) ? t.Display : n.Label;
            return new NodeView(n, t, NodeStyle.For(n), Sel.Contains(n.Id), Geometry.Layout(n, label), Geometry.ExtraBelow(n));
        });

    /// <summary>Where the inline label editor sits: under the shape for labels drawn below, otherwise inside it.</summary>
    static Rect NodeEditRect(NodeView v) => v.Layout.Below
        ? new Rect(v.N.X + v.N.W / 2 - 80, v.N.Y + v.N.H + 4, 160, 44)
        : new Rect(v.N.X + 8, v.N.Y + 6, v.N.W - 16, v.N.H - 12);

    /// <summary>White backing behind a label drawn below a shape.</summary>
    static Rect LabelBackground(Geometry.LabelLayout lay)
    {
        double w = lay.Lines.Max(l => l.Length) * 7.2 + 14;
        return new Rect(lay.TextX - w / 2, lay.TextY - 14, w, lay.Lines.Count * Geometry.LineHeight + 6);
    }

    static bool HasProperties(Node n) => n.Properties.Values.Any(v => !string.IsNullOrWhiteSpace(v));

    /// <summary>The live connector drawn while dragging from a port (null when not connecting).</summary>
    ConnectPreview? LiveConnector()
    {
        if (mode != Mode.Connect || FindBox(connectFrom) is not { } from) return null;
        IBox? target = HitTest(curX, curY, connectFrom);
        IBox to = target ?? new TextBox { X = curX, Y = curY, W = 0, H = 0 };
        return new ConnectPreview(Geometry.EdgeGeometry(from, to, D.DefaultConnector).D, target);
    }

    /// <summary>Handles on the selected connector: ends (move to another side) and the bend (drag sideways).</summary>
    EdgeHandleView? SelectedEdgeHandles()
    {
        if (SelectedEdgeObj is not { } he || FindBox(he.From) is not { } ha || FindBox(he.To) is not { } hb || EditId == he.Id) return null;
        Geometry.EdgeRoute hr = Geometry.Route(ha, hb, he);
        List<SideTarget> sides = new();
        if (mode == Mode.EdgeEnd)
        {
            IBox box = edgeDragFrom ? ha : hb;
            string? current = edgeDragFrom ? hr.FromSide : hr.ToSide;
            foreach (string side in Sides.All)
            {
                (double sx, double sy) = Geometry.SidePoint(box, side);
                sides.Add(new SideTarget(sx, sy, side == current));
            }
        }
        (double bx, double by) = hr.BendHandle;
        (double p0x, double p0y) = hr.Points[0];
        (double p1x, double p1y) = hr.Points[^1];
        return new EdgeHandleView(he.Id, sides, hr.BendIndex >= 0 && he.Style != EdgeStyles.Straight, bx, by, hr.BendIsVertical, p0x, p0y, p1x, p1y);
    }

    /// <summary>Icon picker contents, filtered by the search box (empty categories dropped).</summary>
    List<(string Category, List<string> Names)> FilteredIcons() =>
        Icons.Categories
            .Select(c => (c.Category, Names: c.Names.Where(nm => string.IsNullOrWhiteSpace(iconFilter) || nm.Contains(iconFilter.Trim(), StringComparison.OrdinalIgnoreCase)).ToList()))
            .Where(c => c.Names.Count > 0)
            .ToList();

    int MemberCount(Group g) => Members(g).Count();

    int OffPageCount => OutsidePage().Count;

    string? LinkExclude => hasFile ? D.Name : null;

    /// <summary>Inline editor styled like the box itself, so editing looks the same as the finished text.</summary>
    static string TextEditStyle(TextBox t, TextStyle s) =>
        $"font-family:{s.Family};font-size:{F(t.FontSize)}px;line-height:{F(t.FontSize * TextLook.LineSpacing)}px;" +
        $"font-weight:{(t.Bold ? "600" : "400")};color:{s.TextColour};text-align:{t.Align};padding:{F(TextLook.PadY)}px {F(TextLook.PadX)}px;";

    // Small UI handlers that used to be inline lambdas in the markup.

    void ToggleExportMenu() => exportMenu = !exportMenu;
    void CloseExportMenu() => exportMenu = false;
    void StartPaletteDrag(string key) => paletteDrag = key;
    void DragOver() { }
    void AskDelete(string name) => confirmDelete = name;
    void DiscardAndNew() { Reset(); CloseDialog(); }
    void FitGroupToContents(Group g) { Change(); FitGroup(g); }

    void SetPropValue(Node n, string key, ChangeEventArgs e)
    {
        n.Properties[key] = e.Value?.ToString() ?? string.Empty;
        MarkDirty();
    }

    void RemoveProp(Node n, string key) { Checkpoint(); n.Properties.Remove(key); }

    void SetEdgeLabel(Edge e, string label) { Checkpoint(); e.Label = label; MarkDirty(); }

    void RenameDiagram(ChangeEventArgs e)
    {
        D.Name = DiagramStore.SafeName(e.Value?.ToString() ?? string.Empty);
        MarkDirty();
    }

    void SetFitOnExport(ChangeEventArgs e) { Change(); D.Page.FitOnExport = e.Value is true; }
}
