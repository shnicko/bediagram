namespace BeDiagram.Services;

/// <summary>
/// A window onto the drawing in world coordinates (what's on screen) plus the on-screen size in px.
/// Exported as-is on an open canvas, or scaled to fit inside the margins when a page size is set.
/// </summary>
public record ExportView(double X, double Y, double W, double H, double ScreenW, double ScreenH);
