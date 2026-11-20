using BeDiagram.Models;

namespace BeDiagram.Editing;

/// <summary>Everyday edits on the selection: join, delete, nudge. (Duplicate is with copy and paste in
/// DiagramEditor.Clipboard.cs.)</summary>
public partial class DiagramEditor
{
    // ───────────── Editing actions ─────────────

    public void JoinSelected()
    {
        if (Selection.Count < 2)
        {
            view.ShowToast("Select two or more items to join");
            return;
        }
        string snap = Snapshot();
        int added = 0;
        for (int i = 0; i < Selection.Count - 1; i++)
        {
            if (AddEdge(Selection[i], Selection[i + 1]))
            {
                added++;
            }
        }
        if (added > 0)
        {
            PushUndo(snap);
            Dirty = true;
        }
        if (added == 0)
        {
            view.ShowToast("Already joined");
        }
        else if (added == 1)
        {
            view.ShowToast("Joined 1 connector");
        }
        else
        {
            view.ShowToast($"Joined {added} connectors");
        }
    }

    public void DeleteSelection()
    {
        if (Selection.Count == 0 && SelectedEdgeId is null)
        {
            return;
        }
        Change();
        if (SelectedEdgeId is not null)
        {
            string edgeId = SelectedEdgeId;
            Diagram.Edges.RemoveAll(e => e.Id == edgeId);
        }
        // Deleting a group deletes its contents too (Ungroup keeps them).
        HashSet<string> ids = new HashSet<string>();
        foreach (IBox b in WithMembers(Selection))
        {
            ids.Add(b.Id);
        }
        Diagram.Nodes.RemoveAll(n => ids.Contains(n.Id));
        Diagram.TextBoxes.RemoveAll(c => ids.Contains(c.Id));
        Diagram.Groups.RemoveAll(g => ids.Contains(g.Id));
        Diagram.Edges.RemoveAll(e => ids.Contains(e.From) || ids.Contains(e.To));
        Steps.Compact();
        ClearSelection();
        EditingId = null;
    }

    /// <summary>
    /// Moves the selection by (<paramref name="dx"/>, <paramref name="dy"/>) (arrow keys). What moves is
    /// <see cref="MoveSetFor"/>; groups carry their contents through their placement (only the boxes move while
    /// <see cref="ShapesLocked"/>). One undo step.
    /// </summary>
    public void Nudge(double dx, double dy)
    {
        if (Selection.Count == 0)
        {
            return;
        }
        MoveSet set = MoveSetFor(Selection, false);
        if (set.Items.Count == 0)
        {
            view.ShowToast("Shapes are locked in place (Arrange → Lock shapes in place)");
            return;
        }
        Change();
        // Each mover's placement moves it, and a group carries what's inside it unless only the boxes move.
        foreach (IBox b in set.Movers)
        {
            b.Placement.MoveBy(b, dx, dy, Tree, set.Carry);
        }
        // A group that stayed put but holds something that moved grows to fit it.
        foreach (IGroupable root in set.Roots)
        {
            GrowAround(root);
        }
    }
}
