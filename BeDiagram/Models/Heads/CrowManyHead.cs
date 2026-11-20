using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Heads;

/// <summary>Crow's foot: many.</summary>
public class CrowManyHead : CrowsFootHead
{
    public override ArrowHead Key
    {
        get
        {
            return ArrowHead.Many;
        }
    }

    public override string Name
    {
        get
        {
            return "Many";
        }
    }

    public override string Hint
    {
        get
        {
            return "Crow's foot: many";
        }
    }

    protected override string Shapes(string colour)
    {
        return Crow(6, colour);
    }
}
