namespace BeDiagram.Components.Pages.Gestures;

/// <summary>A pointer position: in screen px (for panning) and in world coordinates (for everything else).</summary>
public record GesturePoint(double ClientX, double ClientY, double X, double Y);
