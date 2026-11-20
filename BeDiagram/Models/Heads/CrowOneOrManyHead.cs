using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Heads;

/// <summary>Crow's foot: one or many.</summary>
public class CrowOneOrManyHead : CrowsFootHead
{
    public override ArrowHead Key
    {
        get
        {
            return ArrowHead.OneOrMany;
        }
    }

    public override string Name
    {
        get
        {
            return "One or many";
        }
    }

    public override string Hint
    {
        get
        {
            return "Crow's foot: one or many";
        }
    }

    protected override string Shapes(string colour)
    {
        return Bar(5, colour) + Crow(8, colour);
    }
}
