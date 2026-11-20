using System.Text.Json.Serialization;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models.Catalogue;

/// <summary>
/// The section of a panel a row sits in: input / process / output for an IPO box (<see cref="IpoSections"/>), or the
/// attributes / operations compartment of a class or entity. Saved by name ("output",
/// "attribute"). An unknown name reads as Input. A list's rows have no section (null).
/// </summary>
/// <remarks>Was <c>IpoSection</c>; the member names (and so the saved names) are unchanged, and new members only go
/// on the end so the IPO ones keep their order.</remarks>
[JsonConverter(typeof(EnumNameConverter<RowSection>))]
public enum RowSection
{
    Input,
    Process,
    Output,
    /// <summary>A class's or entity's attributes (an enumeration's literals).</summary>
    Attribute,
    /// <summary>A class's or interface's operations.</summary>
    Operation,
}
