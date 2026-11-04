using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>
/// Strategy for one export file format (SVG, PNG, PDF, Mermaid…).
/// The format only turns a diagram into bytes; which part of the drawing is exported
/// is decided separately by an <see cref="IExportArea"/>, and <see cref="ExportService"/> puts the two together.
/// To add a format: write a class that implements this interface and register it in Program.cs —
/// it then shows up in the Export menu without touching the Designer.
/// </summary>
public interface IExportFormat
{
    /// <summary>Name shown in the Export menu, e.g. "PNG image".</summary>
    string Name { get; }

    /// <summary>File extension including the dot, e.g. ".png".</summary>
    string Extension { get; }

    /// <summary>
    /// True when the format can export a chosen area (whole page / current view).
    /// False for formats that describe the structure rather than a picture (Mermaid), which get one menu entry.
    /// </summary>
    bool UsesArea { get; }

    /// <summary>
    /// Produces the file. <paramref name="view"/> is the window to export (null = the whole page or drawing).
    /// </summary>
    Task<ExportFile> RenderAsync(Diagram d, ExportView? view, ExportOptions options);
}
