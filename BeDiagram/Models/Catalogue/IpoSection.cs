using System.Text.Json.Serialization;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models.Catalogue;

/// <summary>A section of an input–process–output box. Saved by name ("output"). An unknown name reads as Input.</summary>
[JsonConverter(typeof(EnumNameConverter<IpoSection>))]
public enum IpoSection
{
    Input,
    Process,
    Output,
}
