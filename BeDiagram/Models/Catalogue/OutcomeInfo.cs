namespace BeDiagram.Models.Catalogue;

/// <summary>
/// One result of a review or check. The outline differs per outcome (not just the colour), so a sheet printed
/// in greyscale or read by a colour-blind reviewer still tells them apart.
/// </summary>
public record OutcomeInfo(string Key, string Display, string Shape, string Glyph, string Color, string Hint);
