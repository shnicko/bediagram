namespace BeDiagram.Models.Shapes;

/// <summary>Circle (and the Rework outcome). With an icon: icon in the middle, label underneath.</summary>
public class CircleOutline : ShapeOutline
{
    public override string Key => "circle";

    public override string Path(IBox b)
    {
        double r = Math.Min(b.W, b.H) / 2, cx = b.X + b.W / 2, cy = b.Y + b.H / 2;
        return $"M{F(cx - r)},{F(cy)} A{F(r)},{F(r)} 0 1 1 {F(cx + r)},{F(cy)} A{F(r)},{F(r)} 0 1 1 {F(cx - r)},{F(cy)} Z";
    }

    public override (double X, double Y) Boundary(IBox b, double tx, double ty) => RadialBoundary(b, tx, ty);
    public override (double X, double Y) LinkBadgeCentre(IBox b) => RadialBadge(b);

    public override double TextWrap => 0.78;
    public override bool IconMovesLabelBelow => true;
    public override string Mermaid(string label) => $"(({label}))";
}
