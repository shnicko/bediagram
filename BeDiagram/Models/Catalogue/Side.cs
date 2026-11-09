using System.Text.Json.Serialization;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models.Catalogue;

/// <summary>A side of a shape, where a connector end attaches. Saved by name ("right").</summary>
[JsonConverter(typeof(EnumNameConverter<Side>))]
public enum Side
{
    Top,
    Right,
    Bottom,
    Left,
}
