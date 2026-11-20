using System.Text.Json.Serialization;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models.Catalogue;

/// <summary>Who can see a class member (UML). Saved by name ("private"). Optional on a row: null shows no symbol.</summary>
[JsonConverter(typeof(EnumNameConverter<Visibility>))]
public enum Visibility
{
    /// <summary>+</summary>
    Public,
    /// <summary>−</summary>
    Private,
    /// <summary>#</summary>
    Protected,
    /// <summary>~ (visible inside its package).</summary>
    Package,
}
