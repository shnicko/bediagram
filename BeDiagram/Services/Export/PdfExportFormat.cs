using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>Vector PDF, drawn from the SVG export (see <see cref="PdfExporter"/>).</summary>
public class PdfExportFormat : IExportFormat
{
    readonly SvgDiagramWriter writer;

    public PdfExportFormat(SvgDiagramWriter writer)
    {
        this.writer = writer;
    }

    public string Name => "PDF";
    public string Extension => ".pdf";
    public bool UsesArea => true;

    public Task<ExportFile> RenderAsync(Diagram d, ExportView? view, ExportOptions options) =>
        Task.FromResult(new ExportFile(PdfExporter.FromSvg(writer.Write(d, view), d.Name)));
}
