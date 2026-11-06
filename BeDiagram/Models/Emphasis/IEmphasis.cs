using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Emphasis;

/// <summary>
/// Strategy pattern: how strongly a shape is drawn — <see cref="MajorEmphasis"/> for the key steps of a flow,
/// <see cref="NormalEmphasis"/> (the default) and <see cref="MinorEmphasis"/> for supporting steps. Everything that
/// differs by emphasis sits here: colours, outline weight, shadow, the halo ring and the label weight. The canvas and
/// the SVG export both go through <see cref="Geometry"/>, which asks the node's emphasis (<see cref="Emphases.Of"/>),
/// so they always match. Emphasis is a look only: it never changes the shape's size or its step number.
/// </summary>
public interface IEmphasis
{
    /// <summary>Stored in the file as <c>"emphasis"</c> ("major" / "minor"); normal is left out.</summary>
    string Key { get; }

    /// <summary>Button text in the panel.</summary>
    string Name { get; }

    /// <summary>Tooltip in the panel.</summary>
    string Hint { get; }

    /// <summary>Draws the shape with a solid fill whatever the node's own <c>Filled</c> setting.</summary>
    bool ForcesSolidFill { get; }

    /// <summary>The colours to draw with, starting from the node's normal colours.</summary>
    NodeStyle Adjust(NodeStyle normal);

    /// <summary>Outline width.</summary>
    double StrokeWidth { get; }

    /// <summary>Opacity of the soft shadow under the shape (0 = none).</summary>
    double ShadowOpacity { get; }

    /// <summary>Draws a thin ring around the outline, a little way out (see <see cref="Geometry.HaloSvg"/>).</summary>
    bool HasHalo { get; }

    /// <summary>SVG font weight of the label, or null for regular.</summary>
    string? LabelWeight(Node n);

    /// <summary>Colour of a label drawn under the shape (circles, outcomes).</summary>
    string BelowLabelColour { get; }
}
