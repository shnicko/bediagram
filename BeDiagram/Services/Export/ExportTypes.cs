namespace BeDiagram.Services.Export;

/// <summary>What the editor is showing right now: pan and zoom plus the canvas size in screen px.</summary>
public record ScreenView(double PanX, double PanY, double Zoom, double CanvasW, double CanvasH);

/// <summary>An exported file: its content plus an optional note for the user (e.g. "scaled down to fit").</summary>
public record ExportFile(byte[] Content, string? Note = null);

/// <summary>
/// Export settings chosen in the Export menu. Held by the editor for the session (not saved in the diagram).
/// </summary>
public class ExportOptions
{
    /// <summary>Index into <see cref="PngExportFormat.Resolutions"/> (0 = screen / 96 dpi, 1 = 2× / 150 dpi, 2 = 3× / 300 dpi).</summary>
    public int PngResolution { get; set; } = 1;

    /// <summary>Leave the PNG background transparent instead of white.</summary>
    public bool PngTransparent { get; set; }
}
