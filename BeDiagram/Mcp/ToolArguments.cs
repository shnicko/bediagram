using System.Text.Json;
using System.Text.Json.Nodes;

namespace BeDiagram.Mcp;

/// <summary>
/// The <c>arguments</c> object of a <c>tools/call</c> request, read by name. Missing or wrong values throw a
/// <see cref="ToolArgumentException"/> naming the argument.
/// </summary>
public class ToolArguments
{
    readonly JsonObject values;

    public ToolArguments(JsonObject? arguments)
    {
        if (arguments is null)
        {
            values = new JsonObject();
        }
        else
        {
            values = arguments;
        }
    }

    /// <summary>A text argument that must be given and not blank.</summary>
    public string Text(string name)
    {
        string? value = OptionalText(name);
        if (value is null || value.Trim().Length == 0)
        {
            throw new ToolArgumentException("The \"" + name + "\" argument is needed.");
        }
        return value;
    }

    /// <summary>A text argument, or null when it wasn't given (or was null).</summary>
    public string? OptionalText(string name)
    {
        JsonNode? node = values[name];
        if (node is null)
        {
            return null;
        }
        if (node is JsonValue v && v.GetValueKind() == JsonValueKind.String)
        {
            return v.GetValue<string>();
        }
        throw new ToolArgumentException("The \"" + name + "\" argument must be text.");
    }

    /// <summary>A true / false argument; false when it wasn't given.</summary>
    public bool Flag(string name)
    {
        JsonNode? node = values[name];
        if (node is null)
        {
            return false;
        }
        if (node is JsonValue v)
        {
            JsonValueKind kind = v.GetValueKind();
            if (kind == JsonValueKind.True)
            {
                return true;
            }
            if (kind == JsonValueKind.False)
            {
                return false;
            }
        }
        throw new ToolArgumentException("The \"" + name + "\" argument must be true or false.");
    }
}
