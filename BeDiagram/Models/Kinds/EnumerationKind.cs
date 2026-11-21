using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Kinds;

/// <summary>A UML enumeration: «enumeration» over its name, then its literals (names only). Handy for lookup
/// values in a data model.</summary>
public class EnumerationKind : ClassKind
{
    static readonly IReadOnlyList<RowSection> LiteralsOnly = new List<RowSection> { RowSection.Attribute };

    public override IReadOnlyList<RowSection> Sections
    {
        get
        {
            return LiteralsOnly;
        }
    }

    public override string? PaletteInnerPath
    {
        get
        {
            return "M0,12 H48 M12,5 H36 M6,18 H20 M6,23 H20";
        }
    }

    public override bool CanBeAbstract
    {
        get
        {
            return false;
        }
    }

    public override string? Stereotype(Node n)
    {
        return "«enumeration»";
    }

    public override string SectionName(RowSection section)
    {
        return "Literals";
    }

    /// <summary>A literal is just a name.</summary>
    public override ModelRowFields Fields(RowSection section)
    {
        return new ModelRowFields(false, false, false, false);
    }

    public override string NewLabel(Node n)
    {
        return "EnumName";
    }

    public override List<ListItem> NewItems()
    {
        return new List<ListItem>
        {
            Row(RowSection.Attribute, "VALUE_1", null, null),
            Row(RowSection.Attribute, "VALUE_2", null, null),
        };
    }

    protected override ListItem NewRow(Node n, RowSection section)
    {
        return Row(RowSection.Attribute, $"VALUE_{RowsIn(n, section).Count + 1}", null, null);
    }
}
