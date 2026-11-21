using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Heads;

/// <summary>Hollow diamond at the whole: UML aggregation.</summary>
public class DiamondHead : MarkerHead
{
    public override ArrowHead Key
    {
        get
        {
            return ArrowHead.Diamond;
        }
    }

    public override string Name
    {
        get
        {
            return "Hollow diamond";
        }
    }

    public override string Hint
    {
        get
        {
            return "Hollow diamond at the whole: UML aggregation";
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
        return Shape("M1,6 L11.5,1 L22,6 L11.5,11 Z", "#ffffff", colour);
    }
}
