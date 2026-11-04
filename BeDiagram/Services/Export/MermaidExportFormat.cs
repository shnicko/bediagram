using System.Text;
using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>Mermaid text — the structure of the diagram, so there is no area to choose.</summary>
public class MermaidExportFormat : IExportFormat
{
    public string Name => "Mermaid text (.mmd)";
    public string Extension => ".mmd";
    public bool UsesArea => false;

    public Task<ExportFile> RenderAsync(Diagram d, ExportView? view, ExportOptions options) =>
        Task.FromResult(new ExportFile(Encoding.UTF8.GetBytes(Exporters.ToMermaid(d))));
}
