using BeDiagram.Models;

namespace BeDiagram.Editing;

/// <summary>Selection, and the boxes a connector runs between.</summary>
public partial class DiagramEditor
{
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
}
