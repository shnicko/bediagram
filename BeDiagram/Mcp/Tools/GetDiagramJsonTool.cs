using BeDiagram.Cli;
using BeDiagram.Editing;

namespace BeDiagram.Mcp.Tools;

/// <summary><c>get_diagram_json</c>: the whole diagram file, exactly as the app saves it.</summary>
public class GetDiagramJsonTool : DiagramTool
{
    public GetDiagramJsonTool(DiagramWorkspace workspace) : base(workspace)
    {
    }

    public override string Name
    {
        get
        {
            return "get_diagram_json";
        }
    }

    public override string Description
    {
        get
        {
            return "Get one diagram's full file JSON (schema bediagram/1), with every look, colour and route. Use it to "
                + "learn the format or before a big edit made with save_diagram_json; for ids and labels, "
                + "describe_diagram is cheaper.";
        }
    }

    public override bool ReadOnly
    {
        get
        {
            return true;
        }
    }

    protected override Task<McpToolResult> Run(DiagramEditor editor, ConsoleEditorView view, ToolArguments args)
    {
        return Task.FromResult(McpToolResult.Ok(editor.ToJson()));
    }
}
