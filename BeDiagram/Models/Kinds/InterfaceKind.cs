using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Kinds;

/// <summary>A UML interface: «interface» over its name, then its operations.</summary>
public class InterfaceKind : ClassKind
{
    static readonly IReadOnlyList<RowSection> OperationsOnly = new List<RowSection> { RowSection.Operation };

    public override IReadOnlyList<RowSection> Sections
    {
        get
        {
            return OperationsOnly;
        }
    }

    public override string? PaletteInnerPath
    {
        get
        {
            return "M0,12 H48 M12,5 H36";
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
        return "«interface»";
    }

    public override string NewLabel(Node n)
    {
        return "InterfaceName";
    }

    public override List<ListItem> NewItems()
    {
        return new List<ListItem> { Row(RowSection.Operation, "execute()", "void", Visibility.Public) };
    }
}
