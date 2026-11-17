namespace BeDiagram.Editing.Guides;

/// <summary>A centre guide drawn while dragging: a vertical or horizontal line in world coordinates, from the dragged
/// item's snap point to the shape centres it lines up with.</summary>
public record GuideLine(double X1, double Y1, double X2, double Y2);
