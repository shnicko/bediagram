using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;

namespace BeDiagram.Mcp.Tools;

/// <summary><c>list_diagrams</c>: the names of every diagram in the storage folder, newest first.</summary>
public class ListDiagramsTool : IMcpTool
{
    readonly DiagramWorkspace workspace;

    public ListDiagramsTool(DiagramWorkspace workspace)
    {
        this.workspace = workspace;
    }

    public string Name
    {
        get
        {
            return "list_diagrams";
        }
    }

    public string Description
    {
        get
        {
            return "List the diagrams in the BeDiagram storage folder, newest first, with when each was last saved. "
                + "The names are what the other tools take as \"diagram\".";
        }
    }

    public bool ReadOnly
    {
        get
        {
            return true;
        }
    }

    public JsonObject InputSchema()
    {
        return new ToolSchema().Build();
    }

    public Task<McpToolResult> CallAsync(ToolArguments args)
    {
        List<(string Name, DateTime Modified)> diagrams = workspace.List();
        StringBuilder text = new StringBuilder();
        text.AppendLine(diagrams.Count + " diagrams in " + workspace.Folder);
        foreach ((string name, DateTime modified) in diagrams)
        {
            text.AppendLine("  " + name + "  (" + modified.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + ")");
        }
        return Task.FromResult(McpToolResult.Ok(text.ToString()));
    }
}
