using BeDiagram.Editing.Lanes;
using BeDiagram.Models;

namespace BeDiagram.Editing;

/// <summary>Sizing by hand: match the size of the first picked item, a group's position and size fields, and stacking
/// lanes edge to edge.</summary>
public partial class DiagramEditor
{
    // ───────────── Match size ─────────────

    /// <summary>
    /// Gives every selected item the width and / or height of the one picked first (the selection keeps its pick order).
    /// Shapes keep their centre, so connectors stay straight; groups and text boxes keep their top-left corner. Nothing
    /// is made smaller than it may be (<see cref="BoxSizes.MinSize"/>), and a list or IPO box keeps the height of its
    /// rows. Groups around a changed item grow to fit it. One undo step.
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
            (double minW, double minH) = BoxSizes.MinSize(b);
            double w = b.W;
            double h = b.H;
            if (width)
            {
                w = Math.Max(minW, first.W);
            }
            // A list / IPO box is always as tall as its rows.
            bool fixedHeight = b is Node panel && panel.Kind.IsPanel;
            if (height && !fixedHeight)
            {
                h = Math.Max(minH, first.H);
            }
            if (b is Node)
            {
                b.X += (b.W - w) / 2;
                b.Y += (b.H - h) / 2;
            }
            b.W = w;
            b.H = h;
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

    // ───────────── A group's position and size ─────────────

    /// <summary>
    /// The group panel's X / Y / W / H fields. A new position moves the group with its contents, like dragging its title
    /// (only the box while <see cref="ShapesLocked"/>); a new size keeps the top-left corner and leaves the contents
    /// where they are. Not smaller than 160 × 100. Groups around it grow to fit. One undo step; none when nothing
    /// changes.
    /// </summary>
    public void SetGroupBounds(Group g, double x, double y, double w, double h)
    {
        w = Math.Max(BoxSizes.GroupMinW, w);
        h = Math.Max(BoxSizes.GroupMinH, h);
        if (x == g.X && y == g.Y && w == g.W && h == g.H)
        {
            return;
        }
        Change();
        if (x != g.X || y != g.Y)
        {
            if (ShapesLocked)
            {
                g.X = x;
                g.Y = y;
            }
            else
            {
                MoveBox(g, x, y);
            }
        }
        g.W = w;
        g.H = h;
        GrowAround(g);
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
            if (ShapesLocked)
            {
                place.Lane.X = place.X;
                place.Lane.Y = place.Y;
            }
            else
            {
                MoveBox(place.Lane, place.X, place.Y);
            }
        }
        string way = "as columns";
        if (LaneStacker.LayoutOf(lanes) == LaneLayouts.Horizontal)
        {
            way = "as rows";
        }
        view.ShowToast($"{lanes.Count} lanes stacked edge to edge {way}");
    }
}
