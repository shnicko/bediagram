using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Kinds;

/// <summary>
/// A database entity (table) for a logical data model: its name, then its columns. Each column shows PK / FK in a
/// gutter on the left, its name, and its data type on the right. Connect a foreign key row to the primary key row it
/// refers to with a crow's-foot connector.
/// </summary>
public class EntityKind : ModelKind
{
    static readonly IReadOnlyList<RowSection> Columns = new List<RowSection> { RowSection.Attribute };

    public override IReadOnlyList<RowSection> Sections
    {
        get
        {
            return Columns;
        }
    }

    public override string? PaletteInnerPath
    {
        get
        {
            return "M0,9 H48 M11,9 V28";
        }
    }

    public override bool KeyGutter
    {
        get
        {
            return true;
        }
    }

    public override string SectionName(RowSection section)
    {
        return "Columns";
    }

    /// <summary>Data type and keys; no UML visibility or multiplicity.</summary>
    public override ModelRowFields Fields(RowSection section)
    {
        return new ModelRowFields(false, true, false, true);
    }

    /// <summary>The column name only: the type goes on the right, the keys in the gutter.</summary>
    public override string RowText(ListItem item, RowSection section)
    {
        string name = item.Label.Replace("\r", "").Replace("\n", " ").Trim();
        if (name.Length == 0)
        {
            return "column";
        }
        return name;
    }

    public override string? RowType(ListItem item)
    {
        if (item.DataType is null)
        {
            return null;
        }
        return item.DataType.Trim();
    }

    public override string? RowKey(ListItem item)
    {
        if (item.PrimaryKey && item.ForeignKey)
        {
            return "PK FK";
        }
        if (item.PrimaryKey)
        {
            return "PK";
        }
        if (item.ForeignKey)
        {
            return "FK";
        }
        return null;
    }

    public override string NewLabel(Node n)
    {
        return "table_name";
    }

    public override List<ListItem> NewItems()
    {
        ListItem id = Row(RowSection.Attribute, "id", "int", null);
        id.PrimaryKey = true;
        return new List<ListItem>
        {
            id,
            Row(RowSection.Attribute, "name", "varchar(100)", null),
        };
    }

    protected override ListItem NewRow(Node n, RowSection section)
    {
        return Row(RowSection.Attribute, $"column_{RowsIn(n, section).Count + 1}", "varchar(50)", null);
    }
}
