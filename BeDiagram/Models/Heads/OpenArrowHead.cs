using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Heads;

/// <summary>Open arrow: UML navigable association, or dependency when dashed.</summary>
public class OpenArrowHead : MarkerHead
{
    public override ArrowHead Key
    {
        get
        {
            return ArrowHead.Open;
        }
    }

    public override string Name
    {
        get
        {
            return "Open arrow";
        }
    }

    public override string Hint
    {
        get
        {
            return "Open arrow: UML navigable association, or dependency when dashed";
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
            return 12;
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
        return Line("M1,1 L12,6 L1,11", colour);
    }
}
