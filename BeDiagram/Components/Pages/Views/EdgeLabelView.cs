using BeDiagram.Models;
using BeDiagram.Models.Layout;

namespace BeDiagram.Components.Pages.Views;

public record EdgeLabelView(Edge E, bool Selected, bool Editing, EdgeGeom Geo, EdgeLabelLayout Layout);
