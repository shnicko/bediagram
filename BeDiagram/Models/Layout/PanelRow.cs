namespace BeDiagram.Models.Layout;

/// <summary>Where one row of a list / IPO node is drawn: its box, one-line title and description, icon, and (when
/// the node numbers its rows) the number centred at <c>NumberX</c> with a divider down <c>DividerX</c>.
/// Shared by the canvas and the SVG export.</summary>
public record PanelRow(ListItem Item, Rect Box, string Title, string? Desc, double TextX, double TitleY, double DescY,
                       bool HasIcon, double IconX, double IconY, string? Number, double NumberX, double DividerX);
