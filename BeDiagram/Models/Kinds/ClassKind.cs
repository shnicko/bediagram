using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Kinds;

/// <summary>A UML class: its name (with {abstract} over it when <see cref="Node.Abstract"/>), then attributes, then
/// operations. Interfaces and enumerations are classes with fewer compartments.</summary>
public class ClassKind : ModelKind
{
    static readonly IReadOnlyList<RowSection> Both = new List<RowSection> { RowSection.Attribute, RowSection.Operation };

    public override IReadOnlyList<RowSection> Sections
    {
        get
        {
            return Both;
        }
    }

    public override string? PaletteInnerPath
    {
        get
        {
            return "M0,9 H48 M0,19 H48";
        }
    }

    public override bool CanBeAbstract
    {
        get
        {
            return true;
        }
    }

    public override string? Stereotype(Node n)
    {
        if (n.Abstract)
        {
            return "{abstract}";
        }
        return null;
    }

    public override string SectionName(RowSection section)
    {
        if (section == RowSection.Operation)
        {
            return "Operations";
        }
        return "Attributes";
    }

    /// <summary>Attributes: visibility, type and multiplicity. Operations: visibility and return type.</summary>
    public override ModelRowFields Fields(RowSection section)
    {
        if (section == RowSection.Operation)
        {
            return new ModelRowFields(true, true, false, false);
        }
        return new ModelRowFields(true, true, true, false);
    }

    /// <summary>A new class is named "ClassName" so it reads as a placeholder, not as the type.</summary>
    public override string NewLabel(Node n)
    {
        return "ClassName";
    }

    public override List<ListItem> NewItems()
    {
        return new List<ListItem>
        {
            Row(RowSection.Attribute, "id", "int", Visibility.Private),
            Row(RowSection.Attribute, "name", "string", Visibility.Private),
            Row(RowSection.Operation, "save()", "bool", Visibility.Public),
        };
    }

    protected override ListItem NewRow(Node n, RowSection section)
    {
        int count = RowsIn(n, section).Count + 1;
        if (section == RowSection.Operation)
        {
            return Row(section, $"operation{count}()", null, Visibility.Public);
        }
        return Row(section, $"attribute{count}", "string", Visibility.Private);
    }
}
