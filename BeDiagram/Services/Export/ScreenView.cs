namespace BeDiagram.Services.Export;

/// <summary>What the editor is showing right now: pan and zoom plus the canvas size in screen px.</summary>
public record ScreenView(double PanX, double PanY, double Zoom, double CanvasW, double CanvasH);
