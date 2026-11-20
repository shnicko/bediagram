using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Heads;

/// <summary>Crow's foot: zero or many.</summary>
public class CrowZeroOrManyHead : CrowsFootHead
{
    public override ArrowHead Key
    {
        get
        {
            return ArrowHead.ZeroOrMany;
        }
    }

    public override string Name
    {
        get
        {
            return "Zero or many";
        }
    }

    public override string Hint
    {
        get
        {
            return "Crow's foot: zero or many";
        }
    }

    protected override string Shapes(string colour)
    {
        return Ring(5, 7, 4, colour) + Crow(10, colour);
    }
}
