namespace BeDiagram.Models.Layout;

/// <summary>The thin line from text moved away from its shape back to the shape's outline
/// (from the edge of the text block at X1, Y1 to the outline at X2, Y2).</summary>
public record LeaderLine(double X1, double Y1, double X2, double Y2);
