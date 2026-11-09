using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Layout;

/// <summary>An IPO section's title (baseline and anchor) and the divider line before it (null for the first section).</summary>
public record PanelSection(IpoSection Key, string Title, double TitleX, double TitleY, string Anchor, string? Divider);
