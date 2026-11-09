using BeDiagram.Models.Serialization;
using System.Text.Json.Serialization;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models;

/// <summary>
/// One row inside a list or input–process–output node: a label, an optional description and icon, and for an IPO box
/// the section it sits in. Connectors can end on a row (<see cref="Edge.FromItem"/> / <see cref="Edge.ToItem"/>), so
/// the id only has to be unique inside its node and is kept when the node is copied.
/// </summary>
public class ListItem
{
    public string Id { get; set; } = Node.NewId("i");
    public string Label { get; set; } = "";

    /// <summary>Smaller text on the row's second line. A blank description is stored as null (left out of the file).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description
    {
        get => description;
        set => description = string.IsNullOrWhiteSpace(value) ? null : value;
    }
    string? description;

    /// <summary>Optional Lucide icon name (see <see cref="Icons"/>), drawn on the left of the row.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; set; }

    /// <summary>IPO boxes only: input, process or output (see <see cref="IpoSections"/>). Null in a list.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(OptionalEnumNameConverter<IpoSection>))]
    public IpoSection? Section { get; set; }

    /// <summary>A copy with the same id (rows are found by node id + row id, so a copied node keeps its row links).</summary>
    public ListItem Copy() => (ListItem)MemberwiseClone();
}
