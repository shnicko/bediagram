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

    /// <summary>The label position slider's range, in percent along the line.</summary>
    public const int LabelPosMinPercent = 5;
    public const int LabelPosMaxPercent = 95;

    public void SetDefaultConnector(EdgeStyle style)
    {
        Diagram.DefaultConnector = style;
        Dirty = true;
        view.ShowToast($"New connectors will be {EnumNames.Of(style)}" + (SelectedEdge is null ? "" : " — use the panel to change the selected one"));
    }

    public void SetEdgeStyle(Edge e, EdgeStyle style)
    {
        if (e.Style == style)
        {
            return;
        }
        Change();
        e.Style = style;
    }

    public void SetLabelPlace(Edge e, LabelPlace place)
    {
        if (e.LabelPlace == place)
        {
            return;
        }
        Change();
        e.LabelPlace = place;
    }

    /// <summary>Panel label box: keeps the text to two lines (e.g. after a paste).</summary>
    public void EdgeLabelTyped()
    {
        Edge? e = SelectedEdge;
        if (e is not null)
        {
            e.Label = EdgeLabels.ClampEdgeLabel(e.Label);
        }
        MarkDirty();
    }

    public void SetLabelOrient(Edge e, LabelOrient orient)
    {
        if (e.LabelOrient == orient)
        {
            return;
        }
        Change();
        e.LabelOrient = orient;
    }

    public void SetLabelPos(Edge e, double pos)
    {
        Change();
        e.LabelPos = pos;
    }

    /// <summary>Moves a connector's label along the line by whole percent steps (the ‹ › buttons beside the slider).
    /// Lands on a whole percent and stays within the slider's 5–95% range.</summary>
    public void NudgeLabelPos(Edge e, int percent)
    {
        int now = (int)Math.Round(e.LabelPos * 100);
        int next = Math.Clamp(now + percent, LabelPosMinPercent, LabelPosMaxPercent);
        double pos = next / 100.0;
        if (pos == e.LabelPos)
        {
            return;
        }
        Change();
        e.LabelPos = pos;
    }

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
        foreach (Edge e in Diagram.Edges)
        {
            e.CopyLookFrom(src);
        }
        Diagram.DefaultLook.CopyLookFrom(src);
        Diagram.DefaultConnector = src.Style;
        view.ShowToast($"Applied to {Diagram.Edges.Count} connectors");
    }

    public void SetEdgeLook(Edge e, Action<Edge> change)
    {
        Change();
        change(e);
    }

    public void MakeDefaultLook(Edge src)
    {
        Dirty = true;
        Diagram.DefaultLook.CopyLookFrom(src);
        Diagram.DefaultConnector = src.Style;
        view.ShowToast("New connectors will use this look");
    }

    public void SetStyleBetweenSelected(EdgeStyle style)
    {
        // Ids of the selected items, with the members of any selected groups
        HashSet<string> ids = new HashSet<string>();
        foreach (IBox b in WithMembers(Selection))
        {
            ids.Add(b.Id);
        }

        // Connectors with both ends inside the selection
        List<Edge> edges = new List<Edge>();
        foreach (Edge e in Diagram.Edges)
        {
            if (ids.Contains(e.From) && ids.Contains(e.To))
            {
                edges.Add(e);
            }
        }

        if (edges.Count == 0)
        {
            view.ShowToast("No connectors between the selected items");
            return;
        }
        Change();
        foreach (Edge e in edges)
        {
            e.Style = style;
        }
        view.ShowToast($"{edges.Count} connector{(edges.Count == 1 ? "" : "s")} set to {EnumNames.Of(style)}");
    }

    public void ReverseEdge(Edge e)
    {
        Change();
        (e.From, e.To) = (e.To, e.From);
        (e.FromSide, e.ToSide) = (e.ToSide, e.FromSide);
        (e.FromItem, e.ToItem) = (e.ToItem, e.FromItem);
    }

    public void SetEdgeLabel(Edge e, string label)
    {
        Checkpoint();
        e.Label = label;
        MarkDirty();
    }
}
