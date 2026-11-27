using System.Text.Json.Nodes;
using BeDiagram.Cli;
using BeDiagram.Editing;
using BeDiagram.Models;

namespace BeDiagram.Mcp;

/// <summary>
/// A tool that works on one existing diagram (Template Method, like the CLI's <c>DiagramFileCommand</c>): every one
/// takes a <c>diagram</c> argument; the base opens it in a <see cref="DiagramEditor"/>, the tool does its part through
/// the editor, and the base saves the file when the tool changed something and didn't fail.
/// </summary>
public abstract class DiagramTool : IMcpTool
{
    protected DiagramWorkspace Workspace { get; }

    protected DiagramTool(DiagramWorkspace workspace)
    {
        Workspace = workspace;
    }

    public abstract string Name { get; }

    public abstract string Description { get; }

    public abstract bool ReadOnly { get; }

    public JsonObject InputSchema()
    {
        ToolSchema schema = new ToolSchema();
        schema.Text("diagram", "The diagram's name as list_diagrams gives it, folders separated by '/'.", true);
        AddArguments(schema);
        return schema.Build();
    }

    /// <summary>The tool's own arguments, after <c>diagram</c>.</summary>
    protected virtual void AddArguments(ToolSchema schema)
    {
    }

    public async Task<McpToolResult> CallAsync(ToolArguments args)
    {
        Diagram d = await Workspace.OpenAsync(args.Text("diagram"));
        ConsoleEditorView view = new ConsoleEditorView();
        DiagramEditor editor = new DiagramEditor(view);
        editor.Load(d);

        McpToolResult result = await Run(editor, view, args);
        if (ReadOnly || result.IsError || !editor.Dirty)
        {
            return result;
        }
        await Workspace.SaveAsync(editor.Diagram);
        return McpToolResult.Ok(result.Text + "\nSaved \"" + editor.Diagram.Name + "\".");
    }

    /// <summary>Does the tool's work on the open diagram. <paramref name="view"/> holds the editor's status messages.</summary>
    protected abstract Task<McpToolResult> Run(DiagramEditor editor, ConsoleEditorView view, ToolArguments args);
}
