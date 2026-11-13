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

    public string Name
    {
        get
        {
            return "PDF";
        }
    }

    public string Extension
    {
        get
        {
            return ".pdf";
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
        string svg = writer.Write(d, view);
        // The document title: the diagram's title when it has one, otherwise its name.
        string title = d.Name;
        if (!string.IsNullOrWhiteSpace(d.Title))
        {
            title = d.Title.Trim();
        }
        byte[] bytes = pdf.FromSvg(svg, title);
        return Task.FromResult(new ExportFile(bytes));
    }
}
