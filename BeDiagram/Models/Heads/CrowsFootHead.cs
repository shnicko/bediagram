using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Models.Heads;

/// <summary>
/// Crow's-foot cardinality ends for entity relationships: bars (one), a ring (zero) and the crow's foot (many), in an
/// 18 × 14 box whose right-hand edge touches the entity. The line runs through to the entity, as the notation draws it.
/// </summary>
public abstract class CrowsFootHead : MarkerHead
{
    public override string Family
    {
        get
        {
            return "Crow's foot";
        }
    }

    protected override double Width
    {
        get
        {
            return 18;
        }
    }

    protected override double Height
    {
        get
        {
            return 14;
        }
    }

    /// <summary>A bar across the line at <paramref name="x"/>.</summary>
    protected static string Bar(double x, string colour)
    {
        return Line($"M{F(x)},1 V13", colour);
    }

    /// <summary>The crow's foot: three prongs spreading from <paramref name="fromX"/> to the entity.</summary>
    protected static string Crow(double fromX, string colour)
    {
        return Line($"M{F(fromX)},7 L18,1 M{F(fromX)},7 L18,13 M{F(fromX)},7 H18", colour);
    }
}
