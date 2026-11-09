using System.Text.Json.Serialization;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models.Catalogue;

/// <summary>How a connector label is turned. Saved by name ("along"). An unknown name reads as Along.</summary>
[JsonConverter(typeof(EnumNameConverter<LabelOrient>))]
public enum LabelOrient
{
    /// <summary>Runs along the line (the default).</summary>
    Along,
    /// <summary>Always level.</summary>
    Horizontal,
    /// <summary>At 90° to the line.</summary>
    Perpendicular,
}
