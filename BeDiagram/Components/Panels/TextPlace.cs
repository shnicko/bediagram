using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Panels;

/// <summary>One side button under "Text position" in the shape panel: the side, its label and its tooltip.</summary>
public record TextPlace(Side Side, string Name, string Tip);
