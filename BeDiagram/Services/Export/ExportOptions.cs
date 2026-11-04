namespace BeDiagram.Services.Export;

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
