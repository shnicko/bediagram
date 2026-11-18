namespace BeDiagram.Models.Styles;

/// <summary>
/// One colour of the <see cref="Palette"/>.
/// Light = pale fill (shapes, groups, text-box backgrounds); Stroke = outline, icon, connector and text-box text / border;
/// Solid = the fill for the solid (presentation) look; OnSolid = label and icon colour on that solid fill (white, or dark
/// where white would be too faint); Text = a group title or other text drawn on the Light fill.
/// </summary>
public record Swatch(string Light, string Stroke, string Solid, string OnSolid, string Text);
