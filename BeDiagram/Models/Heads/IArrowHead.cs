using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Heads;

/// <summary>
/// Strategy pattern: how one kind of connector end (<see cref="ArrowHead"/>) is drawn. Each head is an SVG marker
/// (the line ends at the tip, and the marker is drawn over it, so hollow heads are filled white to hide the line).
/// The canvas and the SVG export both use the same marker markup, and the PDF writer draws markers from the SVG, so
/// a head looks the same everywhere. Reach a head with <see cref="ArrowHeads.Of"/>.
/// </summary>
public interface IArrowHead
{
    ArrowHead Key { get; }

    /// <summary>The name in the pickers ("Hollow triangle").</summary>
    string Name { get; }

    /// <summary>What it means, for the picker's tooltip ("UML inheritance").</summary>
    string Hint { get; }

    /// <summary>Which picker group it is in: "Arrows", "UML" or "Crow's foot".</summary>
    string Family { get; }

    /// <summary>The marker id for this head in a colour and line weight, or null for no head.</summary>
    string? MarkerId(string colour, string weight);

    /// <summary>The &lt;marker&gt; definition for this head in a colour and line weight ("" for no head).</summary>
    string MarkerDef(string colour, string weight);

    /// <summary>The head drawn pointing right with its tip at (<paramref name="tipX"/>, <paramref name="y"/>), for the
    /// small previews in the pickers ("" for no head).</summary>
    string Preview(double tipX, double y, string colour);
}
