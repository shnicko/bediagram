using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Panels;

/// <summary>
/// Strategy pattern: how the inside of a list or input–process–output node ("panel") is laid out and drawn. A title
/// band at the top, then rows of a fixed height: stacked for a list (<see cref="ListLayout"/>), in three sections side
/// by side (<see cref="IpoColumnsLayout"/>) or one under another (<see cref="IpoStackedLayout"/>) for an IPO box.
/// The node's kind picks the layout (<see cref="Kinds.PanelKind.LayoutOf"/>). The canvas and the SVG export both draw
/// from it, and connectors find a row's box here, so a row ends up in the same place everywhere.
/// </summary>
public interface IPanelLayout
{
    /// <summary>The height that fits the rows exactly (the editor sets it after every row change).</summary>
    double NeededHeight(Node n);

    /// <summary>The narrowest the panel can be.</summary>
    double MinWidth(Node n);

    /// <summary>The IPO section titles and dividers (none for a list).</summary>
    List<PanelSection> Sections(Node n);

    /// <summary>Every row, in drawing order, with where its text and icon go.</summary>
    List<PanelRow> Rows(Node n);

    /// <summary>The title as a node label layout: one line, centred in the band, cut short with … when too long.</summary>
    LabelLayout HeaderLabel(Node n, string label);

    /// <summary>Where the inline title editor sits: inside the title band.</summary>
    Rect TitleEditRect(Node n);

    /// <summary>SVG markup for everything inside the panel except its title text: the band, section titles and
    /// dividers, and the rows. Drawn over the outline and under the title, on the canvas and in the export.</summary>
    string Body(Node n, NodeStyle style);

    /// <summary>The row under a world point, as a connectable box, or null when not on a row.</summary>
    ItemBox? RowAt(Node n, double x, double y);

    /// <summary>The row <paramref name="itemId"/> as a connectable box, or null when it has gone.</summary>
    ItemBox? RowBox(Node n, string itemId);

    /// <summary>The ● ports on each row: the middle of its left and right sides.</summary>
    IEnumerable<(double X, double Y, string ItemId)> RowPorts(Node n);
}
