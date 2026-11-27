namespace BeDiagram.Mcp;

/// <summary>
/// A tool's arguments are missing or wrong (no such diagram, an unknown shape type, …). The server turns it into an
/// error result whose text is the message, so write the message for the model: say what to do instead.
/// </summary>
public class ToolArgumentException : Exception
{
    public ToolArgumentException(string message) : base(message)
    {
    }
}
