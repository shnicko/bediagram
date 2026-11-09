using BeDiagram.Models.Layout;
using BeDiagram.Models.Shapes;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Kinds;

/// <summary>
/// Strategy pattern: everything that depends on what <em>kind</em> of node a shape is: a classic shape, a circle, an
/// outcome, a list or an input–process–output box. That covers whether it's a step, which outline and icon it's drawn
/// with, where its label goes, its colours, and what a new or retyped node of that kind starts with.
/// Each <see cref="Catalogue.NodeTypeInfo"/> holds its kind, and a node reaches it through <see cref="Node.Kind"/>.
/// Ask the kind rather than testing <c>n.Type == NodeType.Outcome</c> elsewhere. A new per-kind behaviour is a new
/// member here, with its usual answer in <see cref="NodeKind"/>.
/// </summary>
/// <remarks>
/// Kinds hold no state (one shared instance per kind). Anything that differs per <em>outline</em> (path, badges,
/// label wrap) stays in <see cref="Shapes.IShapeOutline"/>; the kind only says which outline to use.
/// </remarks>
public interface INodeKind
{
    /// <summary>Takes part in the step order (<see cref="Node.Step"/>). Outcomes are results of a step, not steps.</summary>
    bool IsStep { get; }

    /// <summary>Can also be drawn as a circle (<see cref="Node.Round"/>): the eight classic shapes.</summary>
    bool CanBeRound { get; }

    /// <summary>A titled box holding rows (<see cref="Node.Items"/>): lists and IPO boxes.</summary>
    bool IsPanel { get; }

    /// <summary>Carries an <see cref="Node.Outcome"/> (pass, fail, …) that sets its shape, glyph and colour.</summary>
    bool HasOutcome { get; }

    /// <summary>Extra lines drawn inside the palette tile (e.g. a list's title band and rows), or null.</summary>
    string? PaletteInnerPath { get; }

    /// <summary>Icon stroke width in 24-unit icon space.</summary>
    double IconStroke { get; }

    /// <summary>The outline the node is drawn with (one of the shared <see cref="ShapeOutlines"/>).</summary>
    IShapeOutline OutlineOf(Node n);

    /// <summary>Drawn as a circle with an icon (a Circle, or a classic shape with <see cref="Node.Round"/>):
    /// such labels are semibold.</summary>
    bool DrawnAsCircle(Node n);

    /// <summary>The icon drawn in the node: an outcome's glyph, otherwise the chosen icon.</summary>
    string? IconOf(Node n);

    /// <summary>The label sits underneath the shape (an icon fills the middle).</summary>
    bool LabelBelow(Node n);

    /// <summary>Where the label lines (and the icon, if any) go. The description is added by <see cref="TextLayout.Layout(Node, string)"/>.</summary>
    LabelLayout LayoutLabel(Node n, string label);

    /// <summary>Fill, outline and text colours from the type (or outcome), palette colour and fill, before emphasis.</summary>
    NodeStyle Colours(Node n, bool filled);

    /// <summary>What this node is called: the type's name, or the outcome's name.</summary>
    string NameOf(Node n);

    /// <summary>Heading of the side panel when the node is selected.</summary>
    string PanelTitle(Node n);

    /// <summary>The label a new node of this kind starts with (<see cref="NodeFactory"/>).</summary>
    string NewLabel(Node n);

    /// <summary>The label a node gets when its type changes to this kind and it still had its old type's name.</summary>
    string RetypeLabel(Node n);

    /// <summary>Smallest width and height a resize may leave (a panel can't get shorter than its rows).</summary>
    (double W, double H) MinSize(Node n);

    /// <summary>Where the inline label editor sits, or null for the usual place (inside the shape, or under it).</summary>
    Rect? LabelEditRect(Node n);

    /// <summary>SVG markup drawn inside the node between its outline and its label (a panel's band and rows), or null.</summary>
    string? Body(Node n, NodeStyle style);

    /// <summary>What a connector end on this node attaches to: the row <paramref name="itemId"/> of a panel
    /// (an <see cref="ItemBox"/>), or the node itself (no row asked for, no rows, or the row has gone).</summary>
    IBox EndAt(Node n, string? itemId);

    /// <summary>The row under a world point, as a connectable box, or null (not on a row, or no rows).</summary>
    ItemBox? RowAt(Node n, double x, double y);

    /// <summary>The ● ports on each row (the middle of its left and right sides); none for kinds without rows.</summary>
    IEnumerable<(double X, double Y, string ItemId)> RowPorts(Node n);

    /// <summary>The rows a new list / IPO node starts with; null for kinds without rows.</summary>
    List<ListItem>? NewItems();

    /// <summary>Finishes a new node (<see cref="NodeFactory.Create"/>): <paramref name="outcome"/> is the outcome tile
    /// it came from, or null.</summary>
    void Init(Node n, OutcomeType? outcome);

    /// <summary>A node whose type has just changed to this kind: set or clear what the kind needs (outcome, icon).</summary>
    void Adopt(Node n);
}
