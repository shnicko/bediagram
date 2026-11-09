using System.Text.Json.Serialization;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models.Catalogue;

/// <summary>The result an outcome node shows. Saved by name ("fail"). An unknown name reads as Pass.</summary>
[JsonConverter(typeof(EnumNameConverter<OutcomeType>))]
public enum OutcomeType
{
    Pass,
    Fail,
    Rework,
    Conditional,
    Hold,
}
