using System.Text;
using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>SVG image — page-sized in mm when a page is set, so it drops into Word / PowerPoint at the right size.</summary>
public class SvgExportFormat : IExportFormat
{
    public string Name => "SVG image";
    public string Extension => ".svg";
    public bool UsesArea => true;

    public Task<ExportFile> RenderAsync(Diagram d, ExportView? view, ExportOptions options) =>
        Task.FromResult(new ExportFile(Encoding.UTF8.GetBytes(Exporters.ToSvg(d, view))));
}

/// <summary>Vector PDF, drawn from the SVG export (see <see cref="PdfExporter"/>).</summary>
public class PdfExportFormat : IExportFormat
{
    public string Name => "PDF";
    public string Extension => ".pdf";
    public bool UsesArea => true;

    public Task<ExportFile> RenderAsync(Diagram d, ExportView? view, ExportOptions options) =>
        Task.FromResult(new ExportFile(PdfExporter.FromSvg(Exporters.ToSvg(d, view), d.Name)));
}

/// <summary>Mermaid text — the structure of the diagram, so there is no area to choose.</summary>
public class MermaidExportFormat : IExportFormat
{
    public string Name => "Mermaid text (.mmd)";
    public string Extension => ".mmd";
    public bool UsesArea => false;

    public Task<ExportFile> RenderAsync(Diagram d, ExportView? view, ExportOptions options) =>
        Task.FromResult(new ExportFile(Encoding.UTF8.GetBytes(Exporters.ToMermaid(d))));
}
