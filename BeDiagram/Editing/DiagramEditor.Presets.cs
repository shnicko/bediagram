using BeDiagram.Models;
using BeDiagram.Models.Presets;

namespace BeDiagram.Editing;

/// <summary>
/// Saved styles (<see cref="StylePreset"/>): applying one to selected items, and placing a new item from one (the
/// sidebar's Saved styles list). Each is one undo step. The styles themselves are kept by the
/// <c>StylePresetStore</c> service, outside the diagram.
/// </summary>
public partial class DiagramEditor
{
    // ───────────── Applying a saved style ─────────────

    /// <summary>Copies a shape style onto <paramref name="nodes"/> (look, icon and any saved text). Groups around a
    /// shape whose text changed grow to fit.</summary>
    public void ApplyPreset(IReadOnlyList<Node> nodes, ShapePreset preset)
    {
        if (nodes.Count == 0)
        {
            return;
        }
        Change();
        foreach (Node n in nodes)
        {
            preset.CopyTo(n);
            GrowAround(n);
        }
    }

    /// <summary>Copies a group style onto <paramref name="groups"/> (look and any saved title). Sizes stay as they are.</summary>
    public void ApplyPreset(IReadOnlyList<Group> groups, GroupPreset preset)
    {
        if (groups.Count == 0)
        {
            return;
        }
        Change();
        foreach (Group g in groups)
        {
            preset.CopyTo(g);
        }
    }

    /// <summary>Copies a text box style onto <paramref name="boxes"/> (look and any saved text). A box grows taller
    /// when its text no longer fits.</summary>
    public void ApplyPreset(IReadOnlyList<TextBox> boxes, TextBoxPreset preset)
    {
        if (boxes.Count == 0)
        {
            return;
        }
        Change();
        foreach (TextBox t in boxes)
        {
            preset.CopyTo(t);
            GrowToText(t);
        }
    }

    /// <summary>Copies a connector style onto <paramref name="e"/> (look and any saved label).</summary>
    public void ApplyPreset(Edge e, ConnectorPreset preset)
    {
        Change();
        preset.CopyTo(e);
    }

    // ───────────── Placing a new item from a saved style ─────────────
    // These reuse the usual ways of adding an item (joined to the selection, snapped, in the group it lands in, label
    // open for typing), then style the new item, which is selected, in the same undo step.

    /// <summary>Sidebar drop: a shape from the style, centred on (<paramref name="cx"/>, <paramref name="cy"/>).</summary>
    public void PlacePreset(ShapePreset preset, double cx, double cy)
    {
        AddNodeAt(preset.Item, cx, cy, join: AutoConnect, lockAngle: true);
        StyleNewShape(preset);
    }

    /// <summary>Sidebar click: a shape from the style, placed as the next step after the selection.</summary>
    public void QuickAddPreset(ShapePreset preset)
    {
        QuickAdd(preset.Item);
        StyleNewShape(preset);
    }

    /// <summary>Sidebar drop: a text box (or comment) from the style, centred on the drop point. A comment dropped on a
    /// shape is pinned to it, as with the Comment tile.</summary>
    public void PlacePreset(TextBoxPreset preset, double cx, double cy)
    {
        string? pinTo = null;
        if (preset.IsComment && HitTest(cx, cy) is Node under)
        {
            pinTo = under.Id;
        }
        AddTextAt(cx - preset.W / 2, cy - preset.H / 2, pinTo, preset.IsComment);
        StyleNewTextBox(preset);
    }

    /// <summary>Sidebar click: a text box (or comment) from the style, placed like the Text box / Comment tile places one.</summary>
    public void QuickAddPreset(TextBoxPreset preset)
    {
        if (preset.IsComment)
        {
            AddCommentQuick();
        }
        else
        {
            AddTextQuick();
        }
        StyleNewTextBox(preset);
    }

    /// <summary>Sidebar drop: a group from the style, centred on (<paramref name="cx"/>, <paramref name="cy"/>).</summary>
    public void PlacePreset(GroupPreset preset, double cx, double cy)
    {
        AddGroupAt(cx, cy);
        StyleNewGroup(preset, resize: true);
    }

    /// <summary>Sidebar click: with items selected, wraps them in a group in this style (sized to fit them, like
    /// Ctrl+G); otherwise adds an empty group in this style in the middle of the view.</summary>
    public void QuickAddPreset(GroupPreset preset)
    {
        bool wrapping = false;
        foreach (string id in Selection)
        {
            if (FindBox(id) is IGroupable)
            {
                wrapping = true;
            }
        }
        AddGroupQuick();
        StyleNewGroup(preset, resize: !wrapping);
    }

    /// <summary>The single selected item, when it is a <typeparamref name="T"/> (the item just added); otherwise null.</summary>
    T? NewlyAdded<T>() where T : class
    {
        if (Selection.Count != 1)
        {
            return null;
        }
        return FindBox(Selection[0]) as T;
    }

    void StyleNewShape(ShapePreset preset)
    {
        Node? n = NewlyAdded<Node>();
        if (n is null)
        {
            return;
        }
        preset.CopyTo(n);
        // The saved size, around the same centre; a list or IPO box keeps the height its rows need.
        (double minW, double minH) = n.Placement.MinSize(n);
        double w = Math.Max(minW, preset.W);
        double h = n.H;
        if (!n.Placement.FixedHeight(n))
        {
            h = Math.Max(minH, preset.H);
        }
        if (preset.W > 0 && preset.H > 0)
        {
            n.Placement.Resize(n, w, h);
        }
        GrowAround(n);
    }

    void StyleNewTextBox(TextBoxPreset preset)
    {
        TextBox? t = NewlyAdded<TextBox>();
        if (t is null)
        {
            return;
        }
        preset.CopyTo(t);
        // The saved size, keeping the top centre; never shorter than the text needs.
        if (preset.W > 0 && preset.H > 0)
        {
            (double minW, double minH) = t.Placement.MinSize(t);
            t.Placement.Resize(t, Math.Max(minW, preset.W), Math.Max(minH, preset.H));
        }
        GrowToText(t);
        GrowAround(t);
    }

    void StyleNewGroup(GroupPreset preset, bool resize)
    {
        Group? g = NewlyAdded<Group>();
        if (g is null)
        {
            return;
        }
        preset.CopyTo(g);
        // An empty group takes the saved size, keeping its top-left corner; a group wrapped around items keeps fitting them.
        if (resize && preset.W > 0 && preset.H > 0)
        {
            (double minW, double minH) = g.Placement.MinSize(g);
            g.Placement.Resize(g, Math.Max(minW, preset.W), Math.Max(minH, preset.H));
            GrowAround(g);
        }
    }
}
