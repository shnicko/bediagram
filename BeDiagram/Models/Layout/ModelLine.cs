namespace BeDiagram.Models.Layout;

/// <summary>Where one row of a class or entity is drawn: its box (the full width of the node, for connectors), the
/// text and its baseline, the right-aligned data type (entities) and the key text in the gutter (entities).</summary>
public record ModelLine(ListItem Item, Rect Box, string Text, double TextX, double BaseY, string? Type, double TypeX,
                        string? Key, double KeyX);
