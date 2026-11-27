using BeDiagram.Cli;
using BeDiagram.Editing;
using BeDiagram.Editing.Lanes;

namespace BeDiagram.Mcp.Tools;

/// <summary><c>auto_layout</c>: Auto-layout, or Arrange as swim lanes, like the CLI's <c>layout</c> command, then save.</summary>
public class AutoLayoutTool : DiagramTool
{
    public AutoLayoutTool(DiagramWorkspace workspace) : base(workspace)
    {
    }

    public override string Name
    {
        get
        {
            return "auto_layout";
        }
    }

    public override string Description
    {
        get
        {
            return "Move the shapes into columns by step number (Auto-layout), then tidy the connectors, and save. With "
                + "lanes=true, arrange the top-level groups as swim lanes instead. Direction defaults to the diagram's "
                + "flow. This moves everything, so use it on new or messy diagrams, not on ones laid out by hand.";
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
        schema.Choice("direction", "right = steps run left to right, down = top to bottom, flow = the diagram's own setting.",
            new List<string> { "flow", "right", "down" }, false);
        schema.Flag("lanes", "Arrange the top-level groups as swim lanes instead of laying out everything.");
    }

    protected override Task<McpToolResult> Run(DiagramEditor editor, ConsoleEditorView view, ToolArguments args)
    {
        // The direction asked for, otherwise the diagram's own flow direction.
        ILaneLayout layout = LaneLayouts.ForFlow(editor.Diagram.Flow);
        string? direction = args.OptionalText("direction");
        if (direction == "right")
        {
            layout = LaneLayouts.Horizontal;
        }
        else if (direction == "down")
        {
            layout = LaneLayouts.Vertical;
        }

        if (args.Flag("lanes"))
        {
            editor.ArrangeLanes(layout);
            editor.TidyConnectors();
        }
        else
        {
            editor.AutoLayout(layout);
        }

        string text = "Laid out.";
        if (view.Messages.Count > 0)
        {
            text = string.Join("\n", view.Messages);
        }
        return Task.FromResult(McpToolResult.Ok(text));
    }
}
