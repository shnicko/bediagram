using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Models;
using BeDiagram.Models.Routing;
using BeDiagram.Models.Styles;

namespace BeDiagram.Components.Canvas;

/// <summary>Connectors: under the shapes, or (<see cref="RowLayer"/>) the ones that end on a row of a list / IPO box, drawn above the shapes so that end shows.</summary>
public partial class EdgeLayer : CanvasLayer
{
    /// <summary>Draw the connectors that end on a row of a list / IPO box (the layer above the shapes).</summary>
    [Parameter] public bool RowLayer { get; set; }

    /// <summary>The connectors this layer draws (see <see cref="RowLayer"/>).</summary>
    List<EdgeView> Views()
    {
        List<EdgeView> result = new List<EdgeView>();
        foreach (Edge e in D.Edges)
        {
            if (e.EndsOnRow != RowLayer)
            {
                continue;
            }
            (IBox A, IBox B)? ends = Editor.Ends(e);
            if (ends is null)
            {
                continue;
            }
            (IBox a, IBox b) = ends.Value;
            double lw = LineLook.Width(e.Weight);
            string lc = LineLook.Colour(e.Color);
            result.Add(new EdgeView(e, Editor.SelectedEdgeId == e.Id, EdgeRouter.EdgeGeometry(a, b, e), lw, lc, $"url(#{LineLook.MarkerId(lc, e.Weight)})"));
        }
        return result;
    }
}
