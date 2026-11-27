using BeDiagram.Cli;
using BeDiagram.Editing;

namespace BeDiagram.Mcp.Tools;

/// <summary><c>check_diagram</c>: the CLI's <see cref="DiagramChecker"/> (ids, connectors, crossings, page, overlaps, rows).</summary>
public class CheckDiagramTool : DiagramTool
{
    public CheckDiagramTool(DiagramWorkspace workspace) : base(workspace)
    {
    }

    public override string Name
    {
        get
        {
            return "check_diagram";
        }
    }

    public override string Description
    {
        get
        {
            return "Look for problems that would show on the page: repeated or missing ids, connectors to nothing, "
                + "connectors through shapes, routes Tidy connectors would improve, items off the page, shapes on top "
                + "of each other, list rows cut short. Run it after editing. Changes nothing.";
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
        List<string> problems = new DiagramChecker(editor.Diagram).Check();
        if (problems.Count == 0)
        {
            return Task.FromResult(McpToolResult.Ok("No problems found."));
        }
        return Task.FromResult(McpToolResult.Ok(problems.Count + " problems:\n" + string.Join("\n", problems)));
    }
}
