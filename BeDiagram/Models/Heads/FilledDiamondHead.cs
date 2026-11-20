using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Heads;

/// <summary>Filled diamond at the whole: UML composition.</summary>
public class FilledDiamondHead : MarkerHead
{
    public override ArrowHead Key
    {
        get
        {
            return ArrowHead.FilledDiamond;
        }
    }

    public override string Name
    {
        get
        {
            return "Filled diamond";
        }
    }

    public override string Hint
    {
        get
        {
            return "Filled diamond at the whole: UML composition";
        }
    }

    public override string Family
    {
        get
        {
            return "UML";
        }
    }

    protected override double Width
    {
        get
        {
            return 22;
        }
    }

    protected override double Height
    {
        get
        {
            return 12;
        }
    }

    protected override string Shapes(string colour)
    {
        return Shape("M1,6 L11.5,1 L22,6 L11.5,11 Z", colour, colour);
    }
}
