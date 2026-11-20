using BeDiagram.Models;

namespace BeDiagram.Editing;

/// <summary>
/// Copy, cut and paste of items (Ctrl+C / Ctrl+X / Ctrl+V), and Duplicate, which is a copy pasted 30 px down and
/// right. What's copied (<see cref="CopiedItems"/>) is kept outside the diagram, so it can be pasted into another tab
/// or window. Pasted copies get new ids, keep the connectors between them, go on the end of the step order (parallel
/// steps stay parallel) and are selected. One undo step.
/// </summary>
public partial class DiagramEditor
{
    // ───────────── Copy and paste items ─────────────

    /// <summary>Copies of the selected items (groups with everything inside them) and the connectors between them, or
    /// null when nothing is selected.</summary>
    public CopiedItems? CopySelectedItems()
    {
        if (Selection.Count == 0)
        {
            return null;
        }
        CopiedItems copied = new CopiedItems();
        HashSet<string> ids = new HashSet<string>();
        foreach (IBox b in WithMembers(Selection))
        {
            copied.Boxes.Add(CopiedItems.Keep(b));
            ids.Add(b.Id);
        }
        foreach (Edge e in Diagram.Edges)
        {
            if (ids.Contains(e.From) && ids.Contains(e.To))
            {
                copied.Edges.Add(CopiedItems.Keep(e));
            }
        }
        copied.SelectedIds.AddRange(Selection);
        return copied;
    }

    /// <summary>Copy, then delete the selection (one undo step: the delete's).</summary>
    public CopiedItems? CutSelectedItems()
    {
        CopiedItems? copied = CopySelectedItems();
        if (copied is not null)
        {
            DeleteSelection();
        }
        return copied;
    }

    /// <summary>Duplicate (Ctrl+D): the selection copied and pasted 30 px down and right, in this diagram.</summary>
    public void Duplicate()
    {
        CopiedItems? copied = CopySelectedItems();
        if (copied is null)
        {
            return;
        }
        PasteItems(copied, 30, 30);
    }

    /// <summary>
    /// How far to move pasted copies so they don't sit exactly on top of what's there: none in another diagram, 30 px
    /// down and right of the originals in the one they came from, and 30 px more for each earlier paste still in place.
    /// </summary>
    public double PasteOffset(CopiedItems copied)
    {
        if (copied.Count == 0)
        {
            return 0;
        }
        IBox first = copied.Boxes[0];
        double offset = 0;
        // Step on while something of the same kind already sits exactly where the first copy would go.
        for (int i = 0; i < 100; i++)
        {
            if (!SomethingAt(first, first.X + offset, first.Y + offset))
            {
                return offset;
            }
            offset += 30;
        }
        return offset;
    }

    /// <summary>True when an item of the same kind as <paramref name="like"/> has its top-left corner at (x, y).</summary>
    bool SomethingAt(IBox like, double x, double y)
    {
        if (like is Node)
        {
            foreach (Node n in Diagram.Nodes)
            {
                if (n.X == x && n.Y == y)
                {
                    return true;
                }
            }
            return false;
        }
        if (like is TextBox)
        {
            foreach (TextBox t in Diagram.TextBoxes)
            {
                if (t.X == x && t.Y == y)
                {
                    return true;
                }
            }
            return false;
        }
        foreach (Group g in Diagram.Groups)
        {
            if (g.X == x && g.Y == y)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Adds copies of <paramref name="copied"/> moved by (<paramref name="dx"/>, <paramref name="dy"/>), with new
    /// ids, and selects them. A copy stays in its group when the group was copied too, or when the group is in this
    /// diagram (pasting where it came from); otherwise it lands outside any group. One undo step.
    /// </summary>
    public void PasteItems(CopiedItems copied, double dx, double dy)
    {
        if (copied.Count == 0)
        {
            return;
        }
        Change();
        // Old id → the copy's new id.
        Dictionary<string, string> map = new Dictionary<string, string>();
        // Groups first so members (and nested groups) can be re-pointed at the copies.
        List<(Group Old, Group Copy)> groupCopies = new List<(Group Old, Group Copy)>();
        foreach (IBox b in copied.Boxes)
        {
            if (b is Group g)
            {
                Group gg = g.Copy();
                gg.X += dx;
                gg.Y += dy;
                Diagram.Groups.Add(gg);
                map[g.Id] = gg.Id;
                groupCopies.Add((g, gg));
            }
        }
        // Copies go on the end of the flow in the originals' order; originals that ran in parallel stay parallel.
        int stepBase = Steps.Last;
        int firstCopied = FirstStep(copied);
        foreach ((Group g, Group gg) in groupCopies)
        {
            gg.GroupId = PastedGroupId(map, g.GroupId);
        }
        foreach (IBox b in copied.Boxes)
        {
            if (b is Node n)
            {
                Node nn = n.Copy();
                nn.X += dx;
                nn.Y += dy;
                nn.GroupId = PastedGroupId(map, n.GroupId);
                if (n.Step is not null)
                {
                    nn.Step = stepBase + n.Step.Value - firstCopied + 1;
                }
                else
                {
                    nn.Step = null;
                }
                Diagram.Nodes.Add(nn);
                map[n.Id] = nn.Id;
            }
            else if (b is TextBox t)
            {
                TextBox tt = t.Copy();
                tt.X += dx;
                tt.Y += dy;
                tt.GroupId = PastedGroupId(map, t.GroupId);
                Diagram.TextBoxes.Add(tt);
                map[t.Id] = tt.Id;
            }
        }
        foreach (Edge e in copied.Edges)
        {
            if (map.TryGetValue(e.From, out string? from) && map.TryGetValue(e.To, out string? to))
            {
                Edge copy = new Edge();
                copy.From = from;
                copy.To = to;
                copy.FromItem = e.FromItem;
                copy.ToItem = e.ToItem;
                copy.Label = e.Label;
                copy.LabelPos = e.LabelPos;
                copy.CopyLookFrom(e);
                copy.CopyRouteFrom(e);
                Diagram.Edges.Add(copy);
            }
        }
        Steps.Compact();
        // Select the copies of what was selected.
        List<string> newSelection = new List<string>();
        foreach (string id in copied.SelectedIds)
        {
            if (map.TryGetValue(id, out string? newId))
            {
                newSelection.Add(newId);
            }
        }
        Selection.Clear();
        Selection.AddRange(newSelection);
    }

    /// <summary>The lowest step number among the copied shapes (1 when none has one).</summary>
    static int FirstStep(CopiedItems copied)
    {
        int? first = null;
        foreach (IBox b in copied.Boxes)
        {
            if (b is Node n && n.Step is not null)
            {
                if (first is null || n.Step.Value < first.Value)
                {
                    first = n.Step.Value;
                }
            }
        }
        if (first is null)
        {
            return 1;
        }
        return first.Value;
    }

    /// <summary>The group a pasted copy goes in: the copy of its group, else its own group when that is in this
    /// diagram, else none.</summary>
    string? PastedGroupId(Dictionary<string, string> map, string? old)
    {
        if (old is null)
        {
            return null;
        }
        if (map.TryGetValue(old, out string? copy))
        {
            return copy;
        }
        if (FindGroup(old) is not null)
        {
            return old;
        }
        return null;
    }
}
