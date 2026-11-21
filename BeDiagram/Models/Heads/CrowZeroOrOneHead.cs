using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Heads;

/// <summary>Crow's foot: zero or one.</summary>
public class CrowZeroOrOneHead : CrowsFootHead
{
    public override ArrowHead Key
    {
        get
        {
            return ArrowHead.ZeroOrOne;
        }
    }

    public override string Name
    {
        get
        {
            return "Zero or one";
        }
    }

    public override string Hint
    {
        get
        {
            return "Crow's foot: zero or one";
        }
    }

    protected override string Shapes(string colour)
    {
        return Bar(13, colour) + Ring(5, 7, 4, colour);
    }
}
