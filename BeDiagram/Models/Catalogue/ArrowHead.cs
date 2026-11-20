using System.Text.Json.Serialization;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models.Catalogue;

/// <summary>
/// What is drawn at one end of a connector (see <see cref="Heads.ArrowHeads"/>): an arrow, a UML head (open arrow,
/// hollow triangle, hollow or filled diamond) or a crow's-foot cardinality. Saved by name ("filledDiamond"). An
/// unknown name reads as None.
/// </summary>
[JsonConverter(typeof(EnumNameConverter<ArrowHead>))]
public enum ArrowHead
{
    None,
    /// <summary>The filled arrowhead connectors have always had.</summary>
    Arrow,
    /// <summary>Open arrow (UML navigable association, dependency).</summary>
    Open,
    /// <summary>Hollow triangle (UML inheritance, or realisation when dashed).</summary>
    Triangle,
    /// <summary>Hollow diamond at the whole (UML aggregation).</summary>
    Diamond,
    /// <summary>Filled diamond at the whole (UML composition).</summary>
    FilledDiamond,
    /// <summary>Crow's foot: one (a single bar).</summary>
    One,
    /// <summary>Crow's foot: one and only one (two bars).</summary>
    OnlyOne,
    /// <summary>Crow's foot: zero or one (ring and bar).</summary>
    ZeroOrOne,
    /// <summary>Crow's foot: many.</summary>
    Many,
    /// <summary>Crow's foot: one or many (bar and crow's foot).</summary>
    OneOrMany,
    /// <summary>Crow's foot: zero or many (ring and crow's foot).</summary>
    ZeroOrMany,
}
