using BeDiagram.Editing.Lanes;
using BeDiagram.Models;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Location;

namespace BeDiagram.Editing;

/// <summary>Sizing by hand: match the size of the first picked item, the position and size fields (any item), the size
/// fields for several selected groups, and stacking lanes edge to edge. Where an item is measured from and what a new
/// size keeps is the item's <see cref="IPlacement"/>.</summary>
public partial class DiagramEditor
{
    // ───────────── Match size ─────────────

    /// <summary>
    /// Gives every selected item the width and / or height of the one picked first (the selection keeps its pick order).
    /// Each keeps its set-out point (<see cref="IPlacement.Resize"/>): shapes their centre, so connectors stay straight;
    /// groups and text boxes their top-left corner. Nothing is made smaller than it may be
    /// (<see cref="IPlacement.MinSize"/>), and a list or IPO box keeps the height of its rows. Groups around a changed
    /// item grow to fit it. One undo step.
    /// </summary>
    public void MatchSize(bool width, bool height)
    {
        if (Selection.Count < 2)
        {
            return;
        }
        IBox? first = FindBox(Selection[0]);
        if (first is null)
        {
            return;
        }
        Change();
        List<IBox> changed = new List<IBox>();
        for (int i = 1; i < Selection.Count; i++)
        {
            IBox? b = FindBox(Selection[i]);
            if (b is null)
            {
                continue;
            }
            IPlacement p = b.Placement;
            (double minW, double minH) = p.MinSize(b);
            double w = b.W;
            double h = b.H;
            if (width)
            {
                w = Math.Max(minW, first.W);
            }
            if (height && !p.FixedHeight(b))
            {
                h = Math.Max(minH, first.H);
            }
            p.Resize(b, w, h);
            changed.Add(b);
        }
        foreach (IBox b in changed)
        {
            if (b is IGroupable item)
            {
                GrowAround(item);
            }
        }
        string what = "size";
        if (width && !height)
        {
            what = "width";
        }
        else if (height && !width)
        {
            what = "height";
        }
        view.ShowToast($"{changed.Count} item{(changed.Count == 1 ? "" : "s")} given the {what} of the first one picked");
    }

    // ───────────── Any item's position and size ─────────────

    /// <summary>
    /// The X / Y / W / H fields in the panel for a shape, a text box or a group. <paramref name="fields"/> holds the
    /// item's set-out point and size as typed (<see cref="IPlacement.Fields"/>): the centre of a shape (the middle of
    /// the circle or symbol, not the text under it), the top-left corner of a text box or group. The item's placement
    /// works out the box (<see cref="IPlacement.Plan"/>: not below its smallest size, a list or IPO box keeps the height
    /// of its rows) and puts it there (<see cref="IPlacement.Apply"/>): a new size keeps the set-out point, and a group
    /// carries its contents (only the box while <see cref="ShapesLocked"/>). Typed values apply even while shapes are
    /// locked, like the other explicit commands. Groups around it grow to fit. One undo step; none when nothing changes.
    /// </summary>
    public void SetBounds(IGroupable item, Rect fields)
    {
        IPlacement p = item.Placement;
        Rect? box = p.Plan(item, fields);
        if (box is null)
        {
            return;
        }
        Change();
        p.Apply(item, box, Tree, !ShapesLocked);
        GrowAround(item);
    }

    // ───────────── Several groups' size at once ─────────────

    /// <summary>
    /// The W / H fields in the panel for several selected items: gives every group in <paramref name="groups"/> the
    /// width and / or height typed (null = keep each group's own). Like <see cref="SetBounds"/>, each keeps its
    /// top-left corner (<see cref="IPlacement.Resize"/>), its contents stay put, nothing goes below its smallest size
    /// (160 × 100) and groups around a changed one grow to fit.
    /// One undo step; none when nothing changes.
    /// </summary>
    public void SetGroupsSize(IReadOnlyList<Group> groups, double? width, double? height)
    {
        // Work out the new sizes first, so the undo step is only taken when something changes.
        List<Group> changing = new List<Group>();
        List<(double w, double h)> sizes = new List<(double w, double h)>();
        foreach (Group g in groups)
        {
            (double minW, double minH) = g.Placement.MinSize(g);
            double w = g.W;
            double h = g.H;
            if (width is not null)
            {
                w = Math.Max(minW, width.Value);
            }
            if (height is not null)
            {
                h = Math.Max(minH, height.Value);
            }
            if (w != g.W || h != g.H)
            {
                changing.Add(g);
                sizes.Add((w, h));
            }
        }
        if (changing.Count == 0)
        {
            return;
        }
        Change();
        for (int i = 0; i < changing.Count; i++)
        {
            Group g = changing[i];
            g.Placement.Resize(g, sizes[i].w, sizes[i].h);
        }
        foreach (Group g in changing)
        {
            GrowAround(g);
        }
        view.ShowToast($"{changing.Count} group{(changing.Count == 1 ? "" : "s")} resized");
    }

    // ───────────── Stack lanes edge to edge ─────────────

    /// <summary>
    /// Puts the lanes (the selected top-level groups when two or more are selected, otherwise all of them) edge to edge
    /// in their current order, without changing their size: see <see cref="LaneStacker"/>. Each lane carries its
    /// contents (only the box while <see cref="ShapesLocked"/>). One undo step.
    /// </summary>
    public void StackLanes()
    {
        List<Group> top = TopLevelGroups();
        List<Group> picked = new List<Group>();
        foreach (Group g in top)
        {
            if (Selection.Contains(g.Id))
            {
                picked.Add(g);
            }
        }
        List<Group> lanes = top;
        if (picked.Count >= 2)
        {
            lanes = picked;
        }
        if (lanes.Count < 2)
        {
            view.ShowToast("Stacking needs two or more lanes (top-level groups)");
            return;
        }
        List<LanePlace> plan = LaneStacker.Plan(lanes);
        Change();
        foreach (LanePlace place in plan)
        {
            place.Lane.Placement.MoveTo(place.Lane, place.X, place.Y, Tree, !ShapesLocked);
        }
        string way = "as columns";
        if (LaneStacker.LayoutOf(lanes) == LaneLayouts.Horizontal)
        {
            way = "as rows";
        }
        view.ShowToast($"{lanes.Count} lanes stacked edge to edge {way}");
    }
}
