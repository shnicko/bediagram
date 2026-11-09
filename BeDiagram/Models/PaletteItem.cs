using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models;

/// <summary>
/// A shape the user can add from the palette, a number key or a gesture: a node type, plus the outcome for an outcome
/// tile or <see cref="Round"/> for a classic shape offered as a circle. A plain <see cref="NodeType"/> converts to one,
/// so <c>QuickAdd(NodeType.Process)</c> works.
/// </summary>
public readonly record struct PaletteItem(NodeType Type, OutcomeType? Outcome = null, bool Round = false)
{
    public static implicit operator PaletteItem(NodeType type) => new(type);

    /// <summary>The tile's <c>data-palette</c> name in the page ("process", "outcome:pass", "round:decision"), which
    /// the browser tests use to find it.</summary>
    public string Key =>
        Outcome is { } o ? $"{EnumNames.Of(Type)}:{EnumNames.Of(o)}"
        : Round ? $"round:{EnumNames.Of(Type)}"
        : EnumNames.Of(Type);
}
