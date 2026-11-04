using System.Text.Json.Serialization;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models;

public class Node : IGroupable
{
    public string Id { get; set; } = NewId("n");
    public string Type { get; set; } = NodeTypes.Process;
    public string Label { get; set; } = "";
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 160;
    public double H { get; set; } = 64;
    public string? GroupId { get; set; }
    /// <summary>Optional Lucide icon name (see <see cref="Icons"/>). Circles show it centred; other shapes show it beside the label.</summary>
    public string? Icon { get; set; }
    /// <summary>Optional colour key from <see cref="Palette"/>; null = the type's default colours.</summary>
    public string? Color { get; set; }
    /// <summary>Solid fill with white text/icon (presentation style).</summary>
    public bool Filled { get; set; }
    /// <summary>Outcome nodes only: "pass", "fail", "rework", "conditional" or "hold" (see <see cref="Outcomes"/>).
    /// This carries the meaning; the shape and glyph are just how it is drawn.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Outcome { get; set; }
    public Dictionary<string, string> Properties { get; set; } = new();
    /// <summary>Optional longer note drawn in smaller text under the label. For shapes with the label inside,
    /// it sits under the shape. A blank description is stored as null (left out of the file).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description
    {
        get => description;
        set => description = string.IsNullOrWhiteSpace(value) ? null : value;
    }
    string? description;
    /// <summary>Wrap width (px) for text drawn outside the shape: a label under a circle or outcome, and the description.
    /// Null = automatic (see <see cref="Geometry.TextWidth"/>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? TextWidth { get; set; }
    /// <summary>Name of another saved diagram (without extension) holding this step's more detailed flow. Shows a link badge.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; }

    public static string NewId(string prefix) => $"{prefix}_{Guid.NewGuid().ToString("N")[..8]}";
}
