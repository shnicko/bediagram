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
    /// <summary>A UML class: name, attributes and operations.</summary>
    Class,
    /// <summary>A UML interface: «interface» and its operations.</summary>
    Interface,
    /// <summary>A UML enumeration: «enumeration» and its literals.</summary>
    Enumeration,
    /// <summary>A database entity (table): name and columns with key and data type.</summary>
    Entity,
}
