using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Shapes;
using BeDiagram.Models.Styles;

namespace BeDiagram.Components.Pages.Views;

/// <param name="TextBack">White backing behind text drawn outside the shape (label below / description), if any (null when the node has a transparent text background).</param>
public record NodeView(Node N, NodeTypeInfo Type, IShapeOutline Outline, NodeStyle Style, bool Selected, LabelLayout Layout, double Below, Rect? TextBack);
