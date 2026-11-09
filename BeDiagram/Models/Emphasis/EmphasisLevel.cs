using System.Text.Json.Serialization;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models.Emphasis;

/// <summary>How strongly a shape is drawn. Saved by name ("major"); normal is left out of the file.</summary>
[JsonConverter(typeof(EnumNameConverter<EmphasisLevel>))]
public enum EmphasisLevel
{
    /// <summary>The default: left out of the file.</summary>
    Normal,
    /// <summary>A key step: solid fill and a ring.</summary>
    Major,
    /// <summary>A supporting step: lighter.</summary>
    Minor,
}
