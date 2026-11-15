using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Shapes;
using BeDiagram.Models.Styles;

namespace BeDiagram.Components.Pages.Views;

/// <param name="TextBack">White backing behind text drawn outside the shape (label below / description), if any (null when the node has a transparent text background).</param>
/// <param name="OutsideText">The block of text outside the shape (whether or not it has a backing), or null when there is none.
/// Free text (see <see cref="Node.FreeLabel"/>) can be picked up anywhere in it.</param>
public record NodeView(Node N, NodeTypeInfo Type, IShapeOutline Outline, NodeStyle Style, bool Selected, LabelLayout Layout, double Below, Rect? TextBack, Rect? OutsideText);
