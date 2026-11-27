using BeDiagram.Cli;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Mcp.Tools;

/// <summary><c>update_shape</c>: changes a shape's label, description or type, then saves.</summary>
public class UpdateShapeTool : DiagramTool
{
    public UpdateShapeTool(DiagramWorkspace workspace) : base(workspace)
    {
    }

    public override string Name
    {
        get
        {
            return "update_shape";
        }
    }

    public override string Description
    {
        get
        {
            return "Change one shape's label, description and / or type, and save. Leave out what stays the same; an "
                + "empty description removes it. Changing type keeps the shape's place and connectors.";
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
        schema.Text("id", "The shape's id.", true);
        schema.Text("label", "New label.", false);
        schema.Text("description", "New description (empty to remove it).", false);
        schema.Text("type", "New shape type, as add_shape takes it.", false);
    }

    protected override Task<McpToolResult> Run(DiagramEditor editor, ConsoleEditorView view, ToolArguments args)
    {
        string id = args.Text("id");
        Node? n = editor.FindBox(id) as Node;
        if (n is null)
        {
            throw new ToolArgumentException("There's no shape with id \"" + id + "\". Use describe_diagram to see the ids.");
        }

        string? typeName = args.OptionalText("type");
        if (typeName is not null)
        {
            NodeType? type = EnumNames.Parse<NodeType>(typeName);
            if (type is null)
            {
                throw new ToolArgumentException("\"" + typeName + "\" isn't a shape type.");
            }
            if (type.Value != n.Type)
            {
                editor.ChangeType(n, type.Value);
            }
        }
        editor.SetNodeText(n, args.OptionalText("label"), args.OptionalText("description"));
        if (!editor.Dirty)
        {
            return Task.FromResult(McpToolResult.Ok("Nothing to change: give a label, description or type."));
        }
        return Task.FromResult(McpToolResult.Ok("Updated " + EnumNames.Of(n.Type) + " " + n.Id + " \"" + n.Label + "\"."));
    }
}
