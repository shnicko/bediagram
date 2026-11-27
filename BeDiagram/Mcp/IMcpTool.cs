using System.Text.Json.Nodes;

namespace BeDiagram.Mcp;

/// <summary>
/// One tool the MCP server offers to Claude (Command pattern, like <c>IEditorCommand</c>). The client lists the
/// tools with <c>tools/list</c> (name, description, input schema) and runs one with <c>tools/call</c>. The model only
/// ever sees the name, the description and the schema, so those are written for it to read.
/// </summary>
public interface IMcpTool
{
    /// <summary>The tool's name, e.g. "add_shape": letters, digits and underscores.</summary>
    string Name { get; }

    /// <summary>What the tool does and when to use it, written for the model.</summary>
    string Description { get; }

    /// <summary>True when the tool only reads. Sent as the <c>readOnlyHint</c> annotation, so the client can run it
    /// without asking first.</summary>
    bool ReadOnly { get; }

    /// <summary>The JSON Schema of the tool's arguments (an object with properties). Built fresh on every call.</summary>
    JsonObject InputSchema();

    /// <summary>Runs the tool. Problems the model can fix (a missing argument, an unknown id) come back as an error
    /// result or a <see cref="ToolArgumentException"/>, not as a protocol error.</summary>
    Task<McpToolResult> CallAsync(ToolArguments args);
}
