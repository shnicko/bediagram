using BeDiagram.Models.Kinds;

namespace BeDiagram.Models.Catalogue;

/// <summary>
/// One node type: its key, display name, outline, colours and size, plus what a new node of the type starts with.
/// How the type behaves is its <see cref="Kind"/> (Strategy pattern, see <see cref="INodeKind"/>).
/// </summary>
public record NodeTypeInfo(string Key, string Display, string Shape, string Fill, string Stroke, double W, double H)
{
    /// <summary>How nodes of this type behave: classic shape, circle, outcome, list or IPO box.</summary>
    public required INodeKind Kind { get; init; }

    /// <summary>The icon a new node starts with: start ▶ and end 🏁 so the two ends of a flow read as a pair (End is
    /// green like Start, so the flag is what tells them apart), a placeholder on circles. Null for most types.</summary>
    public string? Icon { get; init; }

    /// <summary>The icon when drawn as a circle (<see cref="Node.Round"/>), so every circle of a type looks the same.</summary>
    public string RoundIcon { get; init; } = "settings";

    /// <summary>The properties a new node starts with (empty values), in file order. Rule checks get the fields the
    /// audit tool reads.</summary>
    public IReadOnlyList<string> PropertyKeys { get; init; } = Array.Empty<string>();

    /// <summary>A new, empty property set for a node of this type.</summary>
    public Dictionary<string, string> NewProperties() => PropertyKeys.ToDictionary(k => k, _ => "");

    /// <summary>Width and height of a new node, or of a classic shape drawn as a circle.</summary>
    public (double W, double H) SizeFor(bool round) => round ? (NodeTypes.RoundSize, NodeTypes.RoundSize) : (W, H);
}
