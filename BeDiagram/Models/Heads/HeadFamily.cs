namespace BeDiagram.Models.Heads;

/// <summary>One group of heads in the pickers ("UML") and its heads, in order.</summary>
public record HeadFamily(string Name, List<IArrowHead> Heads);
