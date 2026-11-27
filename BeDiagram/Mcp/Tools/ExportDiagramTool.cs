using System.Text;
using BeDiagram.Cli;
using BeDiagram.Editing;
using BeDiagram.Services.Export;
using BeDiagram.Services.Export.Pdf;

namespace BeDiagram.Mcp.Tools;

/// <summary>
/// <c>export_diagram</c>: writes the diagram as SVG, PDF or Mermaid next to it in the storage folder, through the same
/// <see cref="IExportFormat"/> classes as the Export menu. Mermaid text is also returned, so the model can read it.
/// </summary>
public class ExportDiagramTool : DiagramTool
{
    readonly List<(string Key, IExportFormat Format)> formats;

    public ExportDiagramTool(DiagramWorkspace workspace) : base(workspace)
    {
        SvgDiagramWriter writer = new SvgDiagramWriter();
        formats = new List<(string Key, IExportFormat Format)>();
        formats.Add(("svg", new SvgExportFormat(writer)));
        formats.Add(("pdf", new PdfExportFormat(writer, new PdfWriter())));
        formats.Add(("mermaid", new MermaidExportFormat()));
    }

    public override string Name
    {
        get
        {
            return "export_diagram";
        }
    }

    public override string Description
    {
        get
        {
            return "Export the diagram's page as SVG, PDF or Mermaid into the storage folder, next to the diagram, and "
                + "return the file's path. For Mermaid the text is returned too. PNG can only be exported from the app.";
        }
    }

    /// <summary>The diagram doesn't change, but a file is written, so the client should ask first.</summary>
    public override bool ReadOnly
    {
        get
        {
            return false;
        }
    }

    protected override void AddArguments(ToolSchema schema)
    {
        List<string> keys = new List<string>();
        foreach ((string key, IExportFormat _) in formats)
        {
            keys.Add(key);
        }
        schema.Choice("format", "The file format.", keys, true);
    }

    protected override async Task<McpToolResult> Run(DiagramEditor editor, ConsoleEditorView view, ToolArguments args)
    {
        string key = args.Text("format");
        IExportFormat? format = null;
        foreach ((string k, IExportFormat f) in formats)
        {
            if (k == key)
            {
                format = f;
            }
        }
        if (format is null)
        {
            throw new ToolArgumentException("\"" + key + "\" isn't an export format: use svg, pdf or mermaid.");
        }

        ExportFile file = await format.RenderAsync(editor.Diagram, null, new ExportOptions());
        string path = await Workspace.SaveExportAsync(editor.Diagram.Name, format.Extension, file.Content);
        string text = "Exported to " + path + ".";
        if (file.Note is not null)
        {
            text = text + " (" + file.Note + ")";
        }
        if (format.MediaType.StartsWith("text/"))
        {
            text = text + "\n\n" + Encoding.UTF8.GetString(file.Content);
        }
        return McpToolResult.Ok(text);
    }
}
