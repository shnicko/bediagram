using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Heads;

/// <summary>Hollow triangle at the parent: UML inheritance, or realisation when dashed.</summary>
public class TriangleHead : MarkerHead
{
    public override ArrowHead Key
    {
        get
        {
            return ArrowHead.Triangle;
        }
    }

    public override string Name
    {
        get
        {
            return "Hollow triangle";
        }
    }

    public override string Hint
    {
        get
        {
            return "Hollow triangle at the parent: UML inheritance, or realisation when dashed";
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
            return 14;
        }
    }

    protected override double Height
    {
        get
        {
            return 14;
        }
    }

    protected override string Shapes(string colour)
    {
        return Shape("M1,1 L14,7 L1,13 Z", "#ffffff", colour);
    }
}
