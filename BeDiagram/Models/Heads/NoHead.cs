using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Heads;

/// <summary>A plain line end.</summary>
public class NoHead : IArrowHead
{
    public ArrowHead Key
    {
        get
        {
            return ArrowHead.None;
        }
    }

    public string Name
    {
        get
        {
            return "None";
        }
    }

    public string Hint
    {
        get
        {
            return "A plain line end (UML association)";
        }
    }

    public string Family
    {
        get
        {
            return "Arrows";
        }
    }

    public string? MarkerId(string colour, string weight)
    {
        return null;
    }

    public string MarkerDef(string colour, string weight)
    {
        return "";
    }

    public string Preview(double tipX, double y, string colour)
    {
        return "";
    }
}
