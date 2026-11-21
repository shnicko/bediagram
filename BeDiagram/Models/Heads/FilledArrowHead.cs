using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Styles;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Models.Heads;

/// <summary>The filled arrowhead connectors have always had: it keeps its own marker (<see cref="LineLook.MarkerDef"/>),
/// so existing diagrams draw exactly as before.</summary>
public class FilledArrowHead : IArrowHead
{
    public ArrowHead Key
    {
        get
        {
            return ArrowHead.Arrow;
        }
    }

    public string Name
    {
        get
        {
            return "Arrow";
        }
    }

    public string Hint
    {
        get
        {
            return "Filled arrowhead (flow)";
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
        return LineLook.MarkerId(colour, weight);
    }

    public string MarkerDef(string colour, string weight)
    {
        return LineLook.MarkerDef(colour, weight);
    }

    public string Preview(double tipX, double y, string colour)
    {
        return $"<path d=\"M{F(tipX - 8)},{F(y - 4)} L{F(tipX)},{F(y)} L{F(tipX - 8)},{F(y + 4)} z\" fill=\"{colour}\"/>";
    }
}
