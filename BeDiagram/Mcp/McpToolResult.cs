namespace BeDiagram.Mcp;

/// <summary>
/// What a tool call gives back: text for the model to read, and whether it failed. A failed call is still a normal
/// <c>tools/call</c> result (with <c>isError</c> set), so the model sees the message and can try again.
/// </summary>
public class McpToolResult
{
    public string Text { get; }

    public bool IsError { get; }

    public McpToolResult(string text, bool isError)
    {
        Text = text;
        IsError = isError;
    }

    public static McpToolResult Ok(string text)
    {
        return new McpToolResult(text, false);
    }

    public static McpToolResult Error(string text)
    {
        return new McpToolResult(text, true);
    }
}
