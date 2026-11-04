using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>Vector PDF, drawn from the SVG export (see <see cref="PdfExporter"/>).</summary>
public class PdfExportFormat : IExportFormat
{
    public string Name => "PDF";
    public string Extension => ".pdf";
    public bool UsesArea => true;

    public Task<ExportFile> RenderAsync(Diagram d, ExportView? view, ExportOptions options) =>
        Task.FromResult(new ExportFile(PdfExporter.FromSvg(Exporters.ToSvg(d, view), d.Name)));
}
