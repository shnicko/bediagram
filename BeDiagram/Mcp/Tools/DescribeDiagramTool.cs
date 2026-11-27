using BeDiagram.Cli;
using BeDiagram.Editing;

namespace BeDiagram.Mcp.Tools;

/// <summary><c>describe_diagram</c>: the diagram as a short outline with every id (<see cref="DiagramOutline"/>).</summary>
public class DescribeDiagramTool : DiagramTool
{
    public DescribeDiagramTool(DiagramWorkspace workspace) : base(workspace)
    {
    }

    public override string Name
    {
        get
        {
            return "describe_diagram";
        }
    }

    public override string Description
    {
        get
        {
            return "Outline one diagram: its settings, every shape (id, type, step, label, position, size, rows), group, "
                + "text box and connector. Use this before editing to get the ids; it is much smaller than get_diagram_json.";
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
        return Task.FromResult(McpToolResult.Ok(DiagramOutline.Of(editor.Diagram)));
    }
}
