namespace BeDiagram.Models.Layout;

/// <summary>
/// Connector label laid out in its own frame (origin on the line, rotated by <see cref="EdgeGeom.LabelAngle"/>).
/// <paramref name="Box"/> is the white backing (inline labels covering the line), else null and the text gets a
/// white halo. <paramref name="Hit"/> covers the text for clicking.
/// </summary>
public record EdgeLabelLayout(List<EdgeLabelLine> Lines, string Anchor, Rect? Box, bool Halo, Rect Hit);
