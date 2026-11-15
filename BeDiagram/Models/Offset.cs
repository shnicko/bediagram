namespace BeDiagram.Models;

/// <summary>A distance from a point, in world px (x to the right, y down). Saved as <c>{ "x": 40, "y": -60 }</c>.
/// Immutable, so a copied node can share it.</summary>
public record Offset(double X, double Y);
