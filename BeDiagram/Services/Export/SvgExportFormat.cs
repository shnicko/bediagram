using System.Text;
using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>SVG image — page-sized in mm when a page is set, so it drops into Word / PowerPoint at the right size.</summary>
public class SvgExportFormat : IExportFormat
{
    readonly SvgDiagramWriter writer;

    public SvgExportFormat(SvgDiagramWriter writer)
    {
        this.writer = writer;
    }

    public string Name => "SVG image";
    public string Extension => ".svg";
    public bool UsesArea => true;

    public Task<ExportFile> RenderAsync(Diagram d, ExportView? view, ExportOptions options) =>
        Task.FromResult(new ExportFile(Encoding.UTF8.GetBytes(writer.Write(d, view))));
}
