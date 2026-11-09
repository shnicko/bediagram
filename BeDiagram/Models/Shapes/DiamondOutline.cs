namespace BeDiagram.Models.Shapes;

/// <summary>Decision: a diamond. Text wraps narrower; an icon goes above the text.</summary>
public class DiamondOutline : ShapeOutline
{

    public override string Path(IBox b)
    {
        double x = b.X, y = b.Y, w = b.W, h = b.H;
        return $"M{F(x + w / 2)},{F(y)} L{F(x + w)},{F(y + h / 2)} L{F(x + w / 2)},{F(y + h)} L{F(x)},{F(y + h / 2)} Z";
    }

    protected override List<(double X, double Y)> Polygon(IBox b) =>
        new() { (b.X + b.W / 2, b.Y), (b.X + b.W, b.Y + b.H / 2), (b.X + b.W / 2, b.Y + b.H), (b.X, b.Y + b.H / 2) };

    public override (double X, double Y) LinkBadgeCentre(IBox b) => (b.X + b.W * 0.75, b.Y + b.H * 0.75);

    public override double TextWrap => 0.62;
    public override double? IconAboveTextWrap => 0.55;
    public override string Mermaid(string label) => $"{{{label}}}";
}
