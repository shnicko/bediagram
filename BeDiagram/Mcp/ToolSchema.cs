using System.Text.Json.Nodes;

namespace BeDiagram.Mcp;

/// <summary>
/// Builds the JSON Schema for a tool's arguments: an object whose properties are text, a choice from a list, or
/// true / false. Each method adds one property and returns the builder, so a schema reads as a list of arguments.
/// </summary>
public class ToolSchema
{
    readonly JsonObject properties = new JsonObject();
    readonly JsonArray required = new JsonArray();

    /// <summary>A text argument.</summary>
    public ToolSchema Text(string name, string description, bool isRequired)
    {
        JsonObject p = new JsonObject();
        p["type"] = "string";
        p["description"] = description;
        Add(name, p, isRequired);
        return this;
    }

    /// <summary>A text argument that must be one of <paramref name="values"/>.</summary>
    public ToolSchema Choice(string name, string description, List<string> values, bool isRequired)
    {
        JsonArray allowed = new JsonArray();
        foreach (string v in values)
        {
            allowed.Add(v);
        }
        JsonObject p = new JsonObject();
        p["type"] = "string";
        p["enum"] = allowed;
        p["description"] = description;
        Add(name, p, isRequired);
        return this;
    }

    /// <summary>A true / false argument (never required: leaving it out means false).</summary>
    public ToolSchema Flag(string name, string description)
    {
        JsonObject p = new JsonObject();
        p["type"] = "boolean";
        p["description"] = description;
        Add(name, p, false);
        return this;
    }

    /// <summary>The finished schema. Call once: the properties move into the returned object.</summary>
    public JsonObject Build()
    {
        JsonObject schema = new JsonObject();
        schema["type"] = "object";
        schema["properties"] = properties;
        schema["required"] = required;
        return schema;
    }

    void Add(string name, JsonObject property, bool isRequired)
    {
        properties[name] = property;
        if (isRequired)
        {
            required.Add(name);
        }
    }
}
