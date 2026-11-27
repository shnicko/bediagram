using BeDiagram.Mcp.Tools;

namespace BeDiagram.Mcp;

/// <summary>The tools the server offers, in the order <c>tools/list</c> gives them (like <c>CommandRegistry</c>).</summary>
public static class McpToolRegistry
{
    public static List<IMcpTool> CreateDefault(DiagramWorkspace workspace)
    {
        List<IMcpTool> tools = new List<IMcpTool>();
        // Reading
        tools.Add(new ListDiagramsTool(workspace));
        tools.Add(new DescribeDiagramTool(workspace));
        tools.Add(new GetDiagramJsonTool(workspace));
        tools.Add(new CheckDiagramTool(workspace));
        // Small edits
        tools.Add(new NewDiagramTool(workspace));
        tools.Add(new AddShapeTool(workspace));
        tools.Add(new ConnectTool(workspace));
        tools.Add(new UpdateShapeTool(workspace));
        // Whole diagrams
        tools.Add(new SaveDiagramJsonTool(workspace));
        tools.Add(new TidyConnectorsTool(workspace));
        tools.Add(new AutoLayoutTool(workspace));
        tools.Add(new ExportDiagramTool(workspace));
        return tools;
    }
}
