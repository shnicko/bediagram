using System.Text.Json.Serialization;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models.Catalogue;

/// <summary>The kind of a shape. Saved by name ("decision"). An unknown name in a file reads as Process.</summary>
[JsonConverter(typeof(EnumNameConverter<NodeType>))]
public enum NodeType
{
    /// <summary>The fallback: listed first, so an unknown name in a file reads as a process.</summary>
    Process,
    Start,
    Decision,
    /// <summary>A rule check (saved as "check").</summary>
    Check,
    Action,
    /// <summary>Data / IFC.</summary>
    Data,
    Status,
    End,
    Circle,
    Outcome,
    List,
    /// <summary>Input / process / output box.</summary>
    Ipo,
}
