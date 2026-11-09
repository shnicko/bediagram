using BeDiagram.Models;
using BeDiagram.Services.Export.Pdf;

namespace BeDiagram.Services.Export;

/// <summary>Vector PDF, drawn from the SVG export by <see cref="PdfWriter"/>.</summary>
public class PdfExportFormat : IExportFormat
{
    readonly SvgDiagramWriter writer;
    readonly PdfWriter pdf;

    public PdfExportFormat(SvgDiagramWriter writer, PdfWriter pdf)
    {
        this.writer = writer;
        this.pdf = pdf;
    }

    public string Name => "PDF";
    public string Extension => ".pdf";
    public bool UsesArea => true;

    public Task<ExportFile> RenderAsync(Diagram d, ExportView? view, ExportOptions options) =>
        Task.FromResult(new ExportFile(pdf.FromSvg(writer.Write(d, view), d.Name)));
}
