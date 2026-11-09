using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Models.Shapes;

/// <summary>Status: a flat hexagon. An icon goes above the text.</summary>
public class HexagonOutline : ShapeOutline
{

    static double Cut(IBox b) => Math.Min(20, b.W / 5);

    public override string Path(IBox b)
    {
        double x = b.X, y = b.Y, w = b.W, h = b.H, k = Cut(b);
        return $"M{F(x + k)},{F(y)} H{F(x + w - k)} L{F(x + w)},{F(y + h / 2)} L{F(x + w - k)},{F(y + h)} H{F(x + k)} L{F(x)},{F(y + h / 2)} Z";
    }

    protected override List<(double X, double Y)> Polygon(IBox b)
    {
        double x = b.X, y = b.Y, w = b.W, h = b.H, k = Cut(b);
        return new() { (x + k, y), (x + w - k, y), (x + w, y + h / 2), (x + w - k, y + h), (x + k, y + h), (x, y + h / 2) };
    }

    public override (double X, double Y) LinkBadgeCentre(IBox b) => (b.X + b.W - Cut(b) / 2, b.Y + b.H * 0.75);

    public override double? IconAboveTextWrap => 0.7;
    public override string Mermaid(string label) => $"{{{{{label}}}}}";
}
