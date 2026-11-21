using BeDiagram.Models;
using BeDiagram.Models.Layout;

namespace BeDiagram.Components.Pages.Views;

/// <param name="StartMarker">Marker reference for the head at the start ("url(#…)"), or null for none.</param>
/// <param name="EndMarker">Marker reference for the head at the end, or null for none.</param>
/// <param name="StartSpot">Where the quick-change spot for the start head sits (a little way along the line).</param>
/// <param name="EndSpot">Where the quick-change spot for the end head sits.</param>
public record EdgeView(Edge E, bool Selected, EdgeGeom Geo, double Width, string Colour, string? StartMarker, string? EndMarker,
                       (double X, double Y) StartSpot, (double X, double Y) EndSpot);
