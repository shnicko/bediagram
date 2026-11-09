using System.Text.Json.Serialization;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models.Catalogue;

/// <summary>How a connector is drawn. Saved by name ("elbow"). An unknown name reads as Elbow.</summary>
[JsonConverter(typeof(EnumNameConverter<EdgeStyle>))]
public enum EdgeStyle
{
    /// <summary>Right-angled (the default).</summary>
    Elbow,
    Straight,
}
