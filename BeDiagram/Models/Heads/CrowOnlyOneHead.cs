using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Heads;

/// <summary>Crow's foot: one and only one.</summary>
public class CrowOnlyOneHead : CrowsFootHead
{
    public override ArrowHead Key
    {
        get
        {
            return ArrowHead.OnlyOne;
        }
    }

    public override string Name
    {
        get
        {
            return "One and only one";
        }
    }

    public override string Hint
    {
        get
        {
            return "Crow's foot: one and only one";
        }
    }

    protected override string Shapes(string colour)
    {
        return Bar(8, colour) + Bar(12, colour);
    }
}
