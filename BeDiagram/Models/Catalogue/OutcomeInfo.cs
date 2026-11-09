using BeDiagram.Models.Shapes;

namespace BeDiagram.Models.Catalogue;

/// <summary>
/// One result of a review or check. The outline differs per outcome (not just the colour), so a sheet printed
/// in greyscale or read by a colour-blind reviewer still tells them apart.
/// </summary>
public record OutcomeInfo(OutcomeType Key, string Display, IShapeOutline Outline, string Glyph, string Color, string Hint)
{
    /// <summary>Mermaid has no seal or octagon, so the export puts this symbol in the label (✓, ✕, …)…</summary>
    public required string MermaidSymbol { get; init; }

    /// <summary>…and draws the closest classic Mermaid shape: its opening and closing brackets, e.g. "((" and "))".</summary>
    public required string MermaidOpen { get; init; }

    public required string MermaidClose { get; init; }
}
