using System.Text.Json.Serialization;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models;

/// <summary>
/// One row inside a list, input–process–output, class or entity node: a label, an optional description and icon, the
/// section it sits in (IPO, class, entity), and for classes and entities the member's visibility, data type,
/// multiplicity and keys. Connectors can end on a row (<see cref="Edge.FromItem"/> / <see cref="Edge.ToItem"/>), so
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
        get
        {
            return description;
        }
        set
        {
            description = string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
    string? description;

    /// <summary>Optional Lucide icon name (see <see cref="Icons"/>), drawn on the left of the row.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; set; }

    /// <summary>The section the row sits in: input, process or output in an IPO box (see <see cref="IpoSections"/>),
    /// attribute or operation in a class or entity. Null in a list.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(OptionalEnumNameConverter<RowSection>))]
    public RowSection? Section { get; set; }

    // ───────────── Classes and entities (structured rows) ─────────────

    /// <summary>Classes: + public, − private, # protected or ~ package. Null (left out of the file) = no symbol.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(OptionalEnumNameConverter<Visibility>))]
    public Visibility? Visibility { get; set; }

    /// <summary>Classes and entities: an attribute's or column's data type ("int", "varchar(50)"), or an operation's
    /// return type. A blank type is stored as null (left out of the file).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DataType
    {
        get
        {
            return dataType;
        }
        set
        {
            dataType = Blank(value);
        }
    }
    string? dataType;

    /// <summary>Classes: how many values an attribute holds ("1", "0..1", "*"), drawn as [0..1]. A blank one is
    /// stored as null (left out of the file).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Multiplicity
    {
        get
        {
            return multiplicity;
        }
        set
        {
            multiplicity = Blank(value);
        }
    }
    string? multiplicity;

    /// <summary>Entities: the column is (part of) the primary key. False is left out of the file.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool PrimaryKey { get; set; }

    /// <summary>Entities: the column refers to another entity's key. False is left out of the file.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ForeignKey { get; set; }

    /// <summary>Null for blank text, otherwise the text as typed (not trimmed, so a space typed in the side panel
    /// stays put; the drawing trims it).</summary>
    static string? Blank(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return value;
    }

    /// <summary>A copy with the same id (rows are found by node id + row id, so a copied node keeps its row links).</summary>
    public ListItem Copy()
    {
        return (ListItem)MemberwiseClone();
    }
}
