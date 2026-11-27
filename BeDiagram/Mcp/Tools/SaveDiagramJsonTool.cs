using System.Text.Json.Nodes;
using BeDiagram.Cli;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Services;

namespace BeDiagram.Mcp.Tools;

/// <summary>
/// <c>save_diagram_json</c>: makes a new diagram, or replaces one, from file JSON. It goes through the editor's
/// <see cref="DiagramEditor.ApplyJson"/> (the app's Edit as JSON), so it is checked by <see cref="DiagramValidator"/>
/// and gets step numbers the same way, then <see cref="DiagramChecker"/> reports anything that would show on the page.
/// </summary>
public class SaveDiagramJsonTool : IMcpTool
{
    readonly DiagramWorkspace workspace;

    public SaveDiagramJsonTool(DiagramWorkspace workspace)
    {
        this.workspace = workspace;
    }

    public string Name
    {
        get
        {
            return "save_diagram_json";
        }
    }

    public string Description
    {
        get
        {
            return "Create a diagram, or replace one, from its full file JSON (the format get_diagram_json gives). Good "
                + "for drawing a whole new diagram or making many changes at once. Replacing an existing diagram needs "
                + "overwrite=true. The JSON is validated first; afterwards the result of check_diagram is included.";
        }
    }

    public bool ReadOnly
    {
        get
        {
            return false;
        }
    }

    public JsonObject InputSchema()
    {
        ToolSchema schema = new ToolSchema();
        schema.Text("diagram", "Name to save as, folders separated by '/'.", true);
        schema.Text("json", "The diagram file JSON (schema bediagram/1).", true);
        schema.Flag("overwrite", "Replace the diagram when one with this name already exists.");
        return schema.Build();
    }

    public async Task<McpToolResult> CallAsync(ToolArguments args)
    {
        string name = DiagramStore.SafeName(args.Text("diagram"));
        string json = args.Text("json");
        if (workspace.Exists(name) && !args.Flag("overwrite"))
        {
            return McpToolResult.Error("\"" + name + "\" already exists. Pass overwrite=true to replace it, or choose another name.");
        }

        Diagram start = new Diagram();
        start.Name = name;
        DiagramEditor editor = new DiagramEditor(new ConsoleEditorView());
        editor.Load(start);
        List<string> errors = editor.ApplyJson(json);
        if (errors.Count > 0)
        {
            return McpToolResult.Error("The JSON wasn't saved:\n" + string.Join("\n", errors));
        }

        string path = await workspace.SaveAsync(editor.Diagram);
        List<string> problems = new DiagramChecker(editor.Diagram).Check();
        string report = "Saved \"" + editor.Diagram.Name + "\" to " + path + ".";
        if (problems.Count == 0)
        {
            return McpToolResult.Ok(report + "\nCheck: no problems found.");
        }
        return McpToolResult.Ok(report + "\nCheck found " + problems.Count + " problems:\n" + string.Join("\n", problems));
    }
}
