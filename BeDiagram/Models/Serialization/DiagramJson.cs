using System.Text.Json;

namespace BeDiagram.Models.Serialization;

/// <summary>
/// The one JSON format for a diagram: camelCase names, indented. Saved files (DiagramStore), undo / redo snapshots
/// (DiagramEditor) and the JSON editor all use it, so what you see in the JSON editor is exactly what is on disk.
/// Enums are saved by name through the converters on their types and properties, not through these options.
/// </summary>
public static class DiagramJson
{
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>The diagram as file JSON.</summary>
    public static string Write(Diagram d)
    {
        return JsonSerializer.Serialize(d, Options);
    }

    /// <summary>A diagram read from file JSON; "null" reads as a new, empty diagram. Throws <see cref="JsonException"/>
    /// when the text isn't valid JSON or doesn't fit the diagram's shape.</summary>
    public static Diagram Read(string json)
    {
        Diagram? d = JsonSerializer.Deserialize<Diagram>(json, Options);
        if (d is null)
        {
            return new Diagram();
        }
        return d;
    }
}
