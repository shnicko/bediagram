namespace BeDiagram.Models.Layout;

/// <summary>Where a text box's lines go, plus the SVG text-anchor. Shared by the canvas and the SVG export.</summary>
public record TextBoxLayout(List<TextLine> Lines, string Anchor, double LineHeight, double ContentHeight);
