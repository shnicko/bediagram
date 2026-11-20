using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Heads;

/// <summary>The relationships offered in the connector panel, UML first, then crow's foot. "From" is where the
/// connector was drawn from.</summary>
public static class Relationships
{
    public static readonly IReadOnlyList<Relationship> All = new List<Relationship>
    {
        new Relationship("Association", "A plain line between two classes", ArrowHead.None, ArrowHead.None, LineLook.Solid),
        new Relationship("Directed", "Navigable association: an open arrow at the class it points to", ArrowHead.None, ArrowHead.Open, LineLook.Solid),
        new Relationship("Inheritance", "Draw from the child to the parent: a hollow triangle at the parent", ArrowHead.None, ArrowHead.Triangle, LineLook.Solid),
        new Relationship("Realisation", "Draw from the class to the interface it implements: a dashed line and a hollow triangle", ArrowHead.None, ArrowHead.Triangle, LineLook.Dashed),
        new Relationship("Dependency", "A dashed line and an open arrow at what is depended on", ArrowHead.None, ArrowHead.Open, LineLook.Dashed),
        new Relationship("Aggregation", "Draw from the whole to the part: a hollow diamond at the whole", ArrowHead.Diamond, ArrowHead.None, LineLook.Solid),
        new Relationship("Composition", "Draw from the whole to the part: a filled diamond at the whole", ArrowHead.FilledDiamond, ArrowHead.None, LineLook.Solid),
        new Relationship("One to many", "Crow's foot: one and only one at the start, zero or many at the end", ArrowHead.OnlyOne, ArrowHead.ZeroOrMany, LineLook.Solid),
        new Relationship("Many to one", "Crow's foot: zero or many at the start, one and only one at the end (a foreign key drawn to its key)", ArrowHead.ZeroOrMany, ArrowHead.OnlyOne, LineLook.Solid),
        new Relationship("One to one", "Crow's foot: one and only one at both ends", ArrowHead.OnlyOne, ArrowHead.OnlyOne, LineLook.Solid),
        new Relationship("Many to many", "Crow's foot: zero or many at both ends", ArrowHead.ZeroOrMany, ArrowHead.ZeroOrMany, LineLook.Solid),
    };
}
