using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Heads;

/// <summary>A named pair of ends and a line pattern for a UML or entity relationship, applied in one click from the
/// connector panel. Only the heads and pattern are saved, so the relationship can always be read back from them.</summary>
public record Relationship(string Name, string Hint, ArrowHead From, ArrowHead To, string Pattern);
