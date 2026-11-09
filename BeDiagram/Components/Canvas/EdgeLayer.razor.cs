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
    IEnumerable<EdgeView> Views()
    {
        foreach (Edge e in D.Edges.Where(e => e.EndsOnRow == RowLayer))
        {
            if (Editor.Ends(e) is not (IBox a, IBox b)) continue;
            double lw = LineLook.Width(e.Weight);
            string lc = LineLook.Colour(e.Color);
            yield return new EdgeView(e, Editor.SelectedEdgeId == e.Id, EdgeRouter.EdgeGeometry(a, b, e), lw, lc, $"url(#{LineLook.MarkerId(lc, e.Weight)})");
        }
    }
}
