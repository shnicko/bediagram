using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Models.Shapes;

/// <summary>Start / End: a stadium (pill) with semicircular ends.</summary>
public class PillOutline : ShapeOutline
{

    public override string Path(IBox b)
    {
        double x = b.X, y = b.Y, w = b.W, h = b.H;
        double r = h / 2;
        return $"M{F(x + r)},{F(y)} H{F(x + w - r)} A{F(r)},{F(r)} 0 0 1 {F(x + w - r)},{F(y + h)} H{F(x + r)} A{F(r)},{F(r)} 0 0 1 {F(x + r)},{F(y)} Z";
    }

    public override (double X, double Y) LinkBadgeCentre(IBox b)
    {
        double r = b.H / 2;
        return (b.X + b.W - r + r * D45, b.Y + b.H / 2 + r * D45);
    }

    public override string Mermaid(string label) => $"([{label}])";
}
