using System.Globalization;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Routing;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Editing;

/// <summary>Connector style, line look and labels (one connector or all between the selection).</summary>
public partial class DiagramEditor
{
    // ───────────── Connector style & labels ─────────────

    public void SetDefaultConnector(EdgeStyle style)
    {
        Diagram.DefaultConnector = style;
        Dirty = true;
        view.ShowToast($"New connectors will be {EnumNames.Of(style)}" + (SelectedEdge is null ? "" : " — use the panel to change the selected one"));
    }

    public void SetEdgeStyle(Edge e, EdgeStyle style) { if (e.Style == style) return; Change(); e.Style = style; }

    public void SetLabelPlace(Edge e, LabelPlace place) { if (e.LabelPlace == place) return; Change(); e.LabelPlace = place; }

    /// <summary>Panel label box: keeps the text to two lines (e.g. after a paste).</summary>
    public void EdgeLabelTyped()
    {
        if (SelectedEdge is { } e) e.Label = EdgeLabels.ClampEdgeLabel(e.Label);
        MarkDirty();
    }

    public void SetLabelOrient(Edge e, LabelOrient orient) { if (e.LabelOrient == orient) return; Change(); e.LabelOrient = orient; }

    public void SetLabelPos(Edge e, double pos) { Change(); e.LabelPos = pos; }

    public void DragLabelPos(Edge e, object? value)
    {
        if (double.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
        {
            e.LabelPos = Math.Clamp(v, 0, 1);
            Dirty = true;
        }
    }

    public void ApplyEdgeLookToAll(Edge src)
    {
        Change();
        foreach (Edge e in Diagram.Edges) e.CopyLookFrom(src);
        Diagram.DefaultLook.CopyLookFrom(src);
        Diagram.DefaultConnector = src.Style;
        view.ShowToast($"Applied to {Diagram.Edges.Count} connectors");
    }

    public void SetEdgeLook(Edge e, Action<Edge> change) { Change(); change(e); }

    public void MakeDefaultLook(Edge src)
    {
        Dirty = true;
        Diagram.DefaultLook.CopyLookFrom(src);
        Diagram.DefaultConnector = src.Style;
        view.ShowToast("New connectors will use this look");
    }

    public void SetStyleBetweenSelected(EdgeStyle style)
    {
        HashSet<string> ids = WithMembers(Selection).Select(b => b.Id).ToHashSet();
        List<Edge> edges = Diagram.Edges.Where(e => ids.Contains(e.From) && ids.Contains(e.To)).ToList();
        if (edges.Count == 0) { view.ShowToast("No connectors between the selected items"); return; }
        Change();
        foreach (Edge e in edges) e.Style = style;
        view.ShowToast($"{edges.Count} connector{(edges.Count == 1 ? "" : "s")} set to {EnumNames.Of(style)}");
    }

    public void ReverseEdge(Edge e)
    {
        Change();
        (e.From, e.To) = (e.To, e.From);
        (e.FromSide, e.ToSide) = (e.ToSide, e.FromSide);
        (e.FromItem, e.ToItem) = (e.ToItem, e.FromItem);
    }

    public void SetEdgeLabel(Edge e, string label) { Checkpoint(); e.Label = label; MarkDirty(); }
}
