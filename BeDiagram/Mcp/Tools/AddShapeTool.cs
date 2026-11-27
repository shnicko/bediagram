using BeDiagram.Cli;
using BeDiagram.Editing;
using BeDiagram.Editing.Lanes;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Kinds;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Serialization;
using BeDiagram.Models.Text;

namespace BeDiagram.Mcp.Tools;

/// <summary>
/// <c>add_shape</c>: adds one shape the way quick-add does in the app: after an existing item in the flow direction
/// and joined to it, or below the drawing when nothing is named. Step numbers follow from the editor.
/// </summary>
public class AddShapeTool : DiagramTool
{
    public AddShapeTool(DiagramWorkspace workspace) : base(workspace)
    {
    }

    public override string Name
    {
        get
        {
            return "add_shape";
        }
    }

    public override string Description
    {
        get
        {
            return "Add a shape and save. With \"after\" (an item id), the shape goes next in the flow direction and a "
                + "connector joins it from that item, like quick-add in the app; otherwise it goes below the drawing, "
                + "unconnected. Returns the new shape's id. Types: start, process, decision, check (a rule check), "
                + "action, data, status, end are flowchart steps; outcome is a small pass / fail / rework / conditional "
                + "/ hold badge; list, ipo and the model types are panels with rows (edit their rows with save_diagram_json).";
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
        List<string> types = new List<string>();
        foreach (NodeTypeInfo t in NodeTypes.All)
        {
            types.Add(EnumNames.Of(t.Key));
        }
        List<string> outcomes = new List<string>();
        foreach (OutcomeInfo o in Outcomes.All)
        {
            outcomes.Add(EnumNames.Of(o.Key));
        }
        schema.Choice("type", "The kind of shape.", types, true);
        schema.Text("label", "The text on the shape. Leave out for the type's usual starting label.", false);
        schema.Text("description", "Smaller text under the label.", false);
        schema.Text("after", "Id of the shape, group or text box this one follows and is joined from.", false);
        schema.Text("connector_label", "Label on the joining connector (e.g. Yes / No after a decision). Needs \"after\".", false);
        schema.Choice("outcome", "For type outcome: which outcome (default pass).", outcomes, false);
        schema.Flag("round", "Draw a start / process / decision / check / action / data / status / end shape as a circle.");
    }

    protected override Task<McpToolResult> Run(DiagramEditor editor, ConsoleEditorView view, ToolArguments args)
    {
        string typeName = args.Text("type");
        NodeType? type = EnumNames.Parse<NodeType>(typeName);
        if (type is null)
        {
            throw new ToolArgumentException("\"" + typeName + "\" isn't a shape type. Use one of the types listed in the schema.");
        }
        INodeKind kind = NodeTypes.Get(type.Value).Kind;

        // Outcome badges say which outcome; other shapes ignore it.
        OutcomeType? outcome = null;
        if (kind.HasOutcome)
        {
            string? outcomeName = args.OptionalText("outcome");
            outcome = EnumNames.Parse<OutcomeType>(outcomeName);
            if (outcome is null)
            {
                outcome = OutcomeType.Pass;
            }
        }
        bool round = args.Flag("round");
        if (round && !kind.CanBeRound)
        {
            throw new ToolArgumentException("A " + typeName + " shape can't be drawn round; leave \"round\" out.");
        }
        PaletteItem item = new PaletteItem(type.Value, outcome, round);

        string? after = args.OptionalText("after");
        string? connectorLabel = args.OptionalText("connector_label");
        if (after is not null)
        {
            if (editor.FindBox(after) is null)
            {
                throw new ToolArgumentException("There's no item with id \"" + after + "\". Use describe_diagram to see the ids.");
            }
            // Quick-add joins the new shape from the selected item.
            editor.SelectOnly(after);
            editor.QuickAdd(item, true);
        }
        else
        {
            if (connectorLabel is not null)
            {
                throw new ToolArgumentException("\"connector_label\" needs \"after\": there is no connector without it.");
            }
            editor.ClearSelection();
            view.Centre = FreeSpot(editor.Diagram);
            editor.QuickAdd(item, false);
        }
        // Quick-add opens the label for typing, as in the app; there is no one typing here.
        editor.EndEdit();

        Node? added = null;
        if (editor.Selection.Count > 0)
        {
            added = editor.FindBox(editor.Selection[editor.Selection.Count - 1]) as Node;
        }
        if (added is null)
        {
            return Task.FromResult(McpToolResult.Error("The shape wasn't added."));
        }
        editor.SetNodeText(added, args.OptionalText("label"), args.OptionalText("description"));

        string joined = "";
        if (after is not null)
        {
            foreach (Edge e in editor.Diagram.Edges)
            {
                if (e.From == after && e.To == added.Id)
                {
                    if (connectorLabel is not null)
                    {
                        editor.SetEdgeLabel(e, connectorLabel);
                    }
                    joined = ", joined from " + after + " by connector " + e.Id;
                }
            }
        }
        string step = "";
        if (added.Step is not null)
        {
            step = ", step " + added.Step.Value;
        }
        return Task.FromResult(McpToolResult.Ok("Added " + EnumNames.Of(added.Type) + " " + added.Id + " \"" + added.Label + "\"" + step + joined + "."));
    }

    /// <summary>Where a shape with nothing to follow goes: below everything on the drawing, or, on an empty page, at
    /// the start of the flow (near the top for flow down, near the left for flow right), centred across it.</summary>
    static (double X, double Y) FreeSpot(Diagram d)
    {
        if (d.Nodes.Count == 0)
        {
            if (!d.Page.Enabled)
            {
                return (300, 150);
            }
            // Work in (flow, cross) coordinates so one rule does both directions.
            ILaneLayout layout = LaneLayouts.ForFlow(d.Flow);
            Rect area = PageHeader.DrawingArea(d);
            (double flowStart, double crossStart) = layout.ToLane(area.X, area.Y);
            (double _, double crossLength) = layout.ToLane(area.W, area.H);
            return layout.FromLane(flowStart + 100, crossStart + crossLength / 2);
        }
        double left = double.MaxValue;
        double right = double.MinValue;
        double bottom = double.MinValue;
        foreach (Node n in d.Nodes)
        {
            left = Math.Min(left, n.X);
            right = Math.Max(right, n.X + n.W);
            bottom = Math.Max(bottom, n.Y + n.H + TextLayout.ExtraBelow(n));
        }
        return ((left + right) / 2, bottom + 100);
    }
}
