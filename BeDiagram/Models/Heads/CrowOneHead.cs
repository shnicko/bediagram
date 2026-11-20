using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Heads;

/// <summary>Crow's foot: one.</summary>
public class CrowOneHead : CrowsFootHead
{
    public override ArrowHead Key
    {
        get
        {
            return ArrowHead.One;
        }
    }

    public override string Name
    {
        get
        {
            return "One";
        }
    }

    public override string Hint
    {
        get
        {
            return "Crow's foot: one";
        }
    }

    protected override string Shapes(string colour)
    {
        return Bar(11, colour);
    }
}
