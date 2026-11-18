using System.Text.Json.Serialization;

namespace BeDiagram.Models.Presets;

/// <summary>
/// A named style the user saved, for quick and consistent use across diagrams. Exactly one of <see cref="Shape"/>,
/// <see cref="Group"/>, <see cref="TextBox"/> and <see cref="Connector"/> is set: the kind of item it was saved from
/// and can be applied to. Applying copies the look once; changing the style later doesn't change items that used it.
/// </summary>
public class StylePreset
{
    public string Id { get; set; } = Node.NewId("s");
    public string Name { get; set; } = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ShapePreset? Shape { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public GroupPreset? Group { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TextBoxPreset? TextBox { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ConnectorPreset? Connector { get; set; }

    /// <summary>True when this and <paramref name="other"/> are for the same kind of item.</summary>
    public bool SameKind(StylePreset other)
    {
        return (Shape is null) == (other.Shape is null)
            && (Group is null) == (other.Group is null)
            && (TextBox is null) == (other.TextBox is null)
            && (Connector is null) == (other.Connector is null);
    }
}
