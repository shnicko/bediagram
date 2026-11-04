using BeDiagram.Models;
using BeDiagram.Models.Layout;

namespace BeDiagram.Components.Pages.Views;

public record EdgeView(Edge E, bool Selected, EdgeGeom Geo, double Width, string Colour, string Marker);
