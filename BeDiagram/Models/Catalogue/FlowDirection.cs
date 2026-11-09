using System.Text.Json.Serialization;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models.Catalogue;

/// <summary>Which way new steps go (quick-add, quick branch, auto-layout default). Saved by name ("down").</summary>
[JsonConverter(typeof(EnumNameConverter<FlowDirection>))]
public enum FlowDirection
{
    /// <summary>New steps go below (the default).</summary>
    Down,
    /// <summary>New steps go to the right.</summary>
    Right,
}
