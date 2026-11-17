using BeDiagram.Models;
using BeDiagram.Models.Location;
using BeDiagram.Models.Styles;

namespace BeDiagram.Editing;

/// <summary>Groups: grouping, nesting (Composite, see GroupTree), growing / fitting groups and membership.</summary>
public partial class DiagramEditor
{
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

    // ───────────── Lock shapes in place ─────────────

    /// <summary>
    /// While on, shapes and text boxes stay where they are: they can't be dragged, nudged or resized, and dragging or
    /// nudging a group moves only its box, not what's inside it. For tidying up group borders at the end of a flow.
    /// Lasts until turned off (not saved with the diagram, no undo step). Explicit commands (align, arrange lanes,
    /// auto-layout) still move shapes.
    /// </summary>
    public bool ShapesLocked { get; private set; }

    /// <summary>Arrange menu / status bar: lock or unlock shapes in place (<see cref="ShapesLocked"/>).</summary>
    public void SetShapesLocked(bool locked)
    {
        ShapesLocked = locked;
        if (locked)
        {
            view.ShowToast("Shapes locked in place: drag group boxes and their grips without moving what's inside");
        }
    }

    /// <summary>
    /// What a drag or nudge of <paramref name="ids"/> moves (<see cref="MoveSet"/>). Normally groups carry their
    /// contents: the items are the selection plus everything inside the groups among it (<see cref="WithMembers"/>).
    /// With <paramref name="boxesOnly"/> (Alt held on a group's title) groups move without their contents. While
    /// <see cref="ShapesLocked"/> only the group boxes move: shapes and text boxes stay. The roots are the items whose
    /// own group isn't moving with them.
    /// </summary>
    public MoveSet MoveSetFor(IEnumerable<string> ids, bool boxesOnly)
    {
        bool carry = !ShapesLocked && !boxesOnly;
        List<IBox> items = new List<IBox>();
        if (carry)
        {
            items = WithMembers(ids);
        }
        else
        {
            HashSet<string> seen = new HashSet<string>();
            foreach (string id in ids)
            {
                IBox? b = FindBox(id);
                if (b is null)
                {
                    continue;
                }
                if (ShapesLocked && b is not Group)
                {
                    continue;
                }
                if (seen.Add(b.Id))
                {
                    items.Add(b);
                }
            }
        }
        // The roots: items whose own group isn't among the items (with carry, what's left once the contents of moving
        // groups are taken out).
        HashSet<string> moving = new HashSet<string>();
        foreach (IBox b in items)
        {
            moving.Add(b.Id);
        }
        List<IGroupable> roots = new List<IGroupable>();
        foreach (IBox b in items)
        {
            if (b is IGroupable item)
            {
                if (item.GroupId is null || !moving.Contains(item.GroupId))
                {
                    roots.Add(item);
                }
            }
        }
        return new MoveSet(items, roots, carry);
    }

    /// <summary>A swim lane (top-level group) whose size is locked (<see cref="Diagram.LockLanes"/>): it doesn't grow or
    /// shrink to fit its items; they stay members even when they hang over its edge.</summary>
    public bool SizeLocked(Group g) => Diagram.LockLanes && g.GroupId is null;

    void GrowOne(Group g)
    {
        if (SizeLocked(g)) return;
        List<IBox> members = Members(g).Cast<IBox>().ToList();
        if (members.Count == 0) return;
        (double x, double y, double w, double h) = Boxes.GroupBounds(members, g);
        double right = Math.Max(g.X + g.W, x + w), bottom = Math.Max(g.Y + g.H, y + h);
        g.X = Math.Min(g.X, x); g.Y = Math.Min(g.Y, y);
        g.W = right - g.X; g.H = bottom - g.Y;
    }

    /// <summary>Shrink-wraps a group around its members (the groups around it grow if they need to).</summary>
    public void FitGroup(Group g) => FitGroup(g, force: false);

    /// <param name="force">The user asked for it ("Fit to contents"): fit even a locked lane.</param>
    void FitGroup(Group g, bool force)
    {
        if (!force && SizeLocked(g)) return;
        List<IBox> members = Members(g).Cast<IBox>().ToList();
        if (members.Count == 0) return;
        (g.X, g.Y, g.W, g.H) = Boxes.GroupBounds(members, g);
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
        (g.X, g.Y, g.W, g.H) = Boxes.GroupBounds(items);
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

    /// <summary>Moves a box's top-left corner to (<paramref name="x"/>, <paramref name="y"/>); a group carries
    /// everything inside it (at any depth) with it (<see cref="IPlacement.MoveTo"/>).</summary>
    public void MoveBox(IBox b, double x, double y)
    {
        b.Placement.MoveTo(b, x, y, Tree, true);
    }

    /// <summary>Moving or restyling the title changes the room it takes inside a group, so each group grows if needed.</summary>
    public void SetGroupTitleLook(IReadOnlyList<Group> groups, string? pos, string? style)
    {
        if (groups.Count == 0) return;
        Change();
        foreach (Group g in groups)
        {
            if (pos is not null) g.HeaderPos = pos;
            if (style is not null) g.HeaderStyle = style;
            GrowGroup(g);
        }
    }

    public void FitGroupToContents(Group g) { Change(); FitGroup(g, force: true); }

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
}
