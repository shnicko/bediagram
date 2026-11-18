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

    public string Name
    {
        get
        {
            return "SVG image";
        }
    }

    public string Extension
    {
        get
        {
            return ".svg";
        }
    }

    /// <summary>The media (MIME) type of what this format makes.</summary>
    public string MediaType
    {
        get
        {
            return "image/svg+xml";
        }
    }

    public bool UsesArea
    {
        get
        {
            return true;
        }
    }

    public Task<ExportFile> RenderAsync(Diagram d, ExportView? view, ExportOptions options)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(writer.Write(d, view));
        return Task.FromResult(new ExportFile(bytes));
    }
}
