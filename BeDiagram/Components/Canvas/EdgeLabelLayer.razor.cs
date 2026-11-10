using BeDiagram.Components.Pages.Views;
using BeDiagram.Models;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Routing;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Components.Canvas;

/// <summary>Connector labels, above the shapes so they are never hidden (inline editor while editing).</summary>
public partial class EdgeLabelLayer : CanvasLayer
{

    /// <summary>Connector labels that need drawing (or editing), in their own layer above the shapes.</summary>
    IEnumerable<EdgeLabelView> EdgeLabelViews()
    {
        foreach (Edge e in D.Edges)
        {
            bool editing = Editor.EditingId == e.Id;
            if (string.IsNullOrWhiteSpace(e.Label) && !editing) continue;
            if (Editor.Ends(e) is not (IBox a, IBox b)) continue;
            EdgeGeom geo = EdgeRouter.EdgeGeometry(a, b, e);
            yield return new EdgeLabelView(e, Editor.SelectedEdgeId == e.Id, editing, geo, EdgeLabels.LayoutEdgeLabel(e, geo));
        }
    }

    static string EdgeLabelTransform(EdgeGeom geo) =>
        $"translate({F(geo.LabelX)},{F(geo.LabelY)}) rotate({F(geo.LabelAngle)})";
}
