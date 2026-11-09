using BeDiagram.Models.Serialization;
using System.Globalization;
using System.Text.Json;
using BeDiagram.Editing.Angles;
using BeDiagram.Editing.Lanes;
using BeDiagram.Editing.Routing;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Kinds;
using BeDiagram.Models.Styles;

namespace BeDiagram.Editing;

/// <summary>
/// The diagram being edited and everything you can do to it: selection, undo / redo (whole-diagram snapshots —
/// the Memento pattern), creating, joining, grouping, aligning, deleting and restyling items.
/// No Blazor, browser or file code: the Designer page shows it and passes user actions in; gestures and commands
/// reach it through the Designer's IEditorContext / ICommandContext. What the editor needs from the screen
/// (a toast, the view centre, focusing the text editor) goes through <see cref="IEditorView"/>.
/// </summary>
public partial class DiagramEditor
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

    /// <summary>Angle lock for moving items and placing joined steps (free, 90°, 45° or 30°). Not saved in the file.</summary>
    public IAngleLock AngleLock { get; set; } = AngleLocks.Free;

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
        // Every shape has a place in the flow; older files (or hand-edited ones) get numbers here.
        Steps.Normalise();
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

    /// <summary>The step order of the open diagram (see <see cref="StepOrder"/>).</summary>
    public StepOrder Steps => new StepOrder(Diagram);

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
            if (Diagram.Nodes[i].Id != exclude && Boxes.Contains(Diagram.Nodes[i], x, y)) return Diagram.Nodes[i];
        for (int i = Diagram.TextBoxes.Count - 1; i >= 0; i--)
            if (Diagram.TextBoxes[i].Id != exclude && Boxes.Contains(Diagram.TextBoxes[i], x, y)) return Diagram.TextBoxes[i];
        // Groups top-most first (inner groups are drawn over their parents), skipping the groups the excluded item is in.
        GroupTree tree = Tree;
        HashSet<string> around = FindBox(exclude) is IGroupable ex ? tree.Ancestors(ex).Select(a => a.Id).ToHashSet() : new HashSet<string>();
        List<Group> order = tree.PaintOrder();
        for (int i = order.Count - 1; i >= 0; i--)
            if (order[i].Id != exclude && !around.Contains(order[i].Id) && Boxes.Contains(order[i], x, y)) return order[i];
        return null;
    }

    /// <summary>The innermost group under a point that <paramref name="item"/> could join (never itself or a group inside it).</summary>
    Group? GroupAt(double x, double y, IGroupable? item = null)
    {
        GroupTree tree = Tree;
        List<Group> order = tree.PaintOrder();
        for (int i = order.Count - 1; i >= 0; i--)
            if (CanHold(tree, order[i], item) && Boxes.Contains(order[i], x, y)) return order[i];
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

    /// <summary>A drag offset after the angle lock (see <see cref="LockedOffset"/> for which axes may still snap).</summary>
    public LockedOffset LockOffset(double dx, double dy) => AngleLock.Apply(dx, dy, Snap);

    /// <summary>
    /// Where a point dragged from (<paramref name="fromX"/>, <paramref name="fromY"/>) lands: on an allowed angle from
    /// the start, snapped to the grid on the axes the lock leaves free. With no lock this is just the snapped point.
    /// </summary>
    public (double X, double Y) LockPoint(double fromX, double fromY, double x, double y)
    {
        LockedOffset o = LockOffset(x - fromX, y - fromY);
        return (o.SnapX ? SnapV(fromX + o.Dx) : fromX + o.Dx, o.SnapY ? SnapV(fromY + o.Dy) : fromY + o.Dy);
    }

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
}
