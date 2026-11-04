namespace BeDiagram.Models.Shapes;

/// <summary>
/// Strategy pattern: everything that depends on a shape's outline — the SVG path, where a straight connector meets
/// it, where the link badge sits, how the label and icon are laid out inside it, and how Mermaid writes it.
/// One class per outline (pill, rect, diamond, …); <see cref="ShapeOutlines"/> finds the one for a shape key, and
/// <see cref="Geometry.OutlineOf(Node)"/> the one a node is drawn with. Canvas and SVG export both go through these,
/// so they always match. To add a shape: write a class (usually derived from <see cref="ShapeOutline"/>) and
/// register it in <see cref="ShapeOutlines"/>.
/// </summary>
public interface IShapeOutline
{
    /// <summary>The shape key used in <see cref="Catalogue.NodeTypes"/> and <see cref="Catalogue.Outcomes"/>, e.g. "diamond".</summary>
    string Key { get; }

    /// <summary>SVG path data for the outline drawn in the box.</summary>
    string Path(IBox b);

    /// <summary>Where a ray from the box centre towards (tx, ty) leaves the outline (the target is not the centre).</summary>
    (double X, double Y) Boundary(IBox b, double tx, double ty);

    /// <summary>Centre of the 🔗 badge: the bottom-right of the drawn outline.</summary>
    (double X, double Y) LinkBadgeCentre(IBox b);

    /// <summary>Label wrap width as a fraction of the shape width (no icon).</summary>
    double TextWrap { get; }

    /// <summary>
    /// For narrow-sided shapes: with an icon, put the icon above the text and wrap the text to this fraction of the
    /// width. Null = icon on the left.
    /// </summary>
    double? IconAboveTextWrap { get; }

    /// <summary>Gap from the left edge to an icon drawn on the left.</summary>
    double IconInset { get; }

    /// <summary>With an icon, draw the icon in the middle and the label underneath (presentation-style circles).</summary>
    bool IconMovesLabelBelow { get; }

    /// <summary>Outcome glyph size as a fraction of the shape's smaller side.</summary>
    double GlyphScale { get; }

    /// <summary>Vertical centre of the outcome glyph as a fraction of the height (a triangle's visual centre is lower).</summary>
    double GlyphCentreY { get; }

    /// <summary>Draws the coloured stripe down the left edge (rule-check shapes).</summary>
    bool HasSideStripe { get; }

    /// <summary>Mermaid node syntax around an already-quoted label, e.g. <c>{"label"}</c> for a diamond.</summary>
    string Mermaid(string label);
}
