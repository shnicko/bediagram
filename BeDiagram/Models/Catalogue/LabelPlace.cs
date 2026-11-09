using System.Text.Json.Serialization;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models.Catalogue;

/// <summary>Where a connector label sits relative to its line. Saved by name ("inline"). An unknown name reads as Inline.</summary>
[JsonConverter(typeof(EnumNameConverter<LabelPlace>))]
public enum LabelPlace
{
    /// <summary>On the line, with a white backing (the default).</summary>
    Inline,
    Above,
    Below,
}
