using BeDiagram.Cli;
using BeDiagram.Editing;
using BeDiagram.Models;

namespace BeDiagram.Mcp.Tools;

/// <summary><c>connect</c>: adds a connector between two items (or rows of list / IPO / model panels), then saves.</summary>
public class ConnectTool : DiagramTool
{
    public ConnectTool(DiagramWorkspace workspace) : base(workspace)
    {
    }

    public override string Name
    {
        get
        {
            return "connect";
        }
    }

    public override string Description
    {
        get
        {
            return "Add a connector from one item to another and save. Items are shapes, groups or text boxes, by id. "
                + "For a list, IPO or model panel, from_row / to_row attach the end to one row (row ids are in "
                + "describe_diagram). The connector takes the diagram's default look.";
        }
    }

    public override bool ReadOnly
    {
        get
        {
            return false;
        }
    }

    protected override void AddArguments(ToolSchema schema)
    {
        schema.Text("from", "Id of the item the connector starts at.", true);
        schema.Text("to", "Id of the item the connector ends at.", true);
        schema.Text("label", "Text on the connector.", false);
        schema.Text("from_row", "Row id in the \"from\" panel to start at.", false);
        schema.Text("to_row", "Row id in the \"to\" panel to end at.", false);
    }

    protected override Task<McpToolResult> Run(DiagramEditor editor, ConsoleEditorView view, ToolArguments args)
    {
        string from = args.Text("from");
        string to = args.Text("to");
        string? label = args.OptionalText("label");
        if (label is null)
        {
            label = "";
        }
        editor.Change();
        bool added = editor.AddEdge(from, to, label, args.OptionalText("from_row"), args.OptionalText("to_row"));
        if (!added)
        {
            return Task.FromResult(McpToolResult.Error("Couldn't connect " + from + " to " + to + ": both ids must exist "
                + "(see describe_diagram), be different, not be a group and something inside it, and not already be "
                + "connected the same way."));
        }
        Edge e = editor.Diagram.Edges[editor.Diagram.Edges.Count - 1];
        return Task.FromResult(McpToolResult.Ok("Added connector " + e.Id + " from " + from + " to " + to + "."));
    }
}
