using BeDiagram.Cli;
using BeDiagram.Editing;

namespace BeDiagram.Mcp.Tools;

/// <summary><c>tidy_connectors</c>: Arrange → Tidy connectors on every connector, then save.</summary>
public class TidyConnectorsTool : DiagramTool
{
    public TidyConnectorsTool(DiagramWorkspace workspace) : base(workspace)
    {
    }

    public override string Name
    {
        get
        {
            return "tidy_connectors";
        }
    }

    public override string Description
    {
        get
        {
            return "Re-route the elbow connectors (which sides they leave and enter, where they bend) to avoid shapes and "
                + "text, exactly like Arrange > Tidy connectors in the app, and save. Shapes don't move.";
        }
    }

    public override bool ReadOnly
    {
        get
        {
            return false;
        }
    }

    protected override Task<McpToolResult> Run(DiagramEditor editor, ConsoleEditorView view, ToolArguments args)
    {
        editor.TidyConnectors();
        return Task.FromResult(McpToolResult.Ok(Messages(view, "Connectors are already tidy.")));
    }

    /// <summary>The editor's status messages, or <paramref name="none"/> when it showed none.</summary>
    static string Messages(ConsoleEditorView view, string none)
    {
        if (view.Messages.Count == 0)
        {
            return none;
        }
        return string.Join("\n", view.Messages);
    }
}
