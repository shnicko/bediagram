namespace BeDiagram.Models.Styles;

/// <summary>Canvas background grid: none, dots or lines. A view setting only: not saved in the file and never exported.</summary>
public static class GridLook
{
    public const string None = "none";
    public const string Dots = "dots";
    public const string Lines = "lines";

    /// <summary>Key, tooltip and toolbar icon for each option.</summary>
    public static readonly (string Key, string Name, string Icon)[] Options =
        { (None, "No grid", "grid-none"), (Dots, "Dot grid", "grid-dots"), (Lines, "Line grid", "grid-3x3") };

    /// <summary>The SVG pattern id for a grid on the open canvas (moves with pan / zoom), or null for none.</summary>
    public static string? CanvasPattern(string grid) => grid switch { Dots => "grid", Lines => "grid-lines", _ => null };

    /// <summary>The SVG pattern id for a grid on the page sheet (world coordinates), or null for none.</summary>
    public static string? PagePattern(string grid) => grid switch { Dots => "grid-world", Lines => "grid-world-lines", _ => null };
}
