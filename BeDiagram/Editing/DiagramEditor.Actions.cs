using BeDiagram.Models;

namespace BeDiagram.Editing;

/// <summary>Everyday edits on the selection: delete, duplicate, nudge, move to front / back.</summary>
public partial class DiagramEditor
{
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
        Steps.Compact();
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
            Group gg = g.Copy();
            gg.X += 30; gg.Y += 30;
            Diagram.Groups.Add(gg); map[g.Id] = gg.Id;
            groupCopies.Add((g, gg));
        }
        string? NewGroup(string? old) => old is not null && map.TryGetValue(old, out string? ng) ? ng : old;
        // Copies go on the end of the flow in the originals' order; originals that ran in parallel stay parallel.
        int stepBase = Steps.Last;
        int firstCopied = items.OfType<Node>().Where(n => n.Step is not null).Select(n => n.Step!.Value).DefaultIfEmpty(1).Min();
        foreach ((Group g, Group gg) in groupCopies) gg.GroupId = NewGroup(g.GroupId);
        foreach (IBox b in items)
        {
            switch (b)
            {
                case Node n:
                    Node nn = n.Copy();
                    nn.X += 30; nn.Y += 30; nn.GroupId = NewGroup(n.GroupId);
                    nn.Step = n.Step is int s ? stepBase + s - firstCopied + 1 : null;
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
                Edge copy = new Edge { From = f, To = t, FromItem = e.FromItem, ToItem = e.ToItem, Label = e.Label, LabelPos = e.LabelPos };
                copy.CopyLookFrom(e);
                copy.CopyRouteFrom(e);
                Diagram.Edges.Add(copy);
            }
        Steps.Compact();
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
}
