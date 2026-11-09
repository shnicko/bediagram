using System.Text;

namespace BeDiagram.Models.Shapes;

/// <summary>Pass outcome: a scalloped seal.</summary>
public class SealOutline : ShapeOutline
{

    public override string Path(IBox b) => SealPath(b.X + b.W / 2, b.Y + b.H / 2, Math.Min(b.W, b.H) / 2);

    public override (double X, double Y) Boundary(IBox b, double tx, double ty) => RadialBoundary(b, tx, ty);
    public override (double X, double Y) LinkBadgeCentre(IBox b) => RadialBadge(b);

    public override double GlyphScale => 0.44;

    /// <summary>Scalloped seal: a ring of semicircular bumps whose tips touch radius <paramref name="r"/>.</summary>
    string SealPath(double cx, double cy, double r)
    {
        const int bumps = 16;
        double step = 2 * Math.PI / bumps;
        // Chord points sit inside so that each bump's tip reaches r.
        double r0 = r / (Math.Cos(step / 2) + Math.Sin(step / 2));
        double bump = r0 * Math.Sin(step / 2);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i <= bumps; i++)
        {
            double a = -Math.PI / 2 + i * step;
            (double px, double py) = (cx + r0 * Math.Cos(a), cy + r0 * Math.Sin(a));
            sb.Append(i == 0 ? $"M{F(px)},{F(py)}" : $" A{F(bump)},{F(bump)} 0 0 1 {F(px)},{F(py)}");
        }
        return sb.Append(" Z").ToString();
    }
}
