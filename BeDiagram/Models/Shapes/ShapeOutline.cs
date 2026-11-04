using System.Globalization;

namespace BeDiagram.Models.Shapes;

/// <summary>
/// Base outline with plain-rectangle behaviour: a rectangle for connector clipping, the badge on the bottom-right
/// corner, label wrapped to 86% of the width, icon on the left. Subclasses override what differs.
/// </summary>
public abstract class ShapeOutline : IShapeOutline
{
    /// <summary>cos 45° — puts badges on the curve of round outlines.</summary>
    protected const double D45 = 0.7071;

    public abstract string Key { get; }
    public abstract string Path(IBox b);

    /// <summary>Corner points used for connector clipping. Default: the box itself.</summary>
    protected virtual List<(double X, double Y)> Polygon(IBox b) =>
        new() { (b.X, b.Y), (b.X + b.W, b.Y), (b.X + b.W, b.Y + b.H), (b.X, b.Y + b.H) };

    public virtual (double X, double Y) Boundary(IBox b, double tx, double ty)
    {
        double cx = b.X + b.W / 2, cy = b.Y + b.H / 2;
        double dx = tx - cx, dy = ty - cy;
        List<(double X, double Y)> poly = Polygon(b);
        double best = double.MaxValue;
        for (int i = 0; i < poly.Count; i++)
        {
            (double p1x, double p1y) = poly[i];
            (double p2x, double p2y) = poly[(i + 1) % poly.Count];
            // Solve centre + t*(dx,dy) = p1 + u*(p2-p1)
            double ex = p2x - p1x, ey = p2y - p1y;
            double den = dx * ey - dy * ex;
            if (Math.Abs(den) < 1e-9) continue;
            double t = ((p1x - cx) * ey - (p1y - cy) * ex) / den;
            double u = ((p1x - cx) * dy - (p1y - cy) * dx) / den;
            if (t > 0 && u >= -1e-9 && u <= 1 + 1e-9 && t < best) best = t;
        }
        return best == double.MaxValue ? (cx, cy) : (cx + dx * best, cy + dy * best);
    }

    public virtual (double X, double Y) LinkBadgeCentre(IBox b) => (b.X + b.W - 1.5, b.Y + b.H - 1.5);

    public virtual double TextWrap => 0.86;
    public virtual double? IconAboveTextWrap => null;
    public virtual double IconInset => 14;
    public virtual bool IconMovesLabelBelow => false;
    public virtual double GlyphScale => 0.48;
    public virtual double GlyphCentreY => 0.5;
    public virtual bool HasSideStripe => false;
    public virtual string Mermaid(string label) => $"[{label}]";

    /// <summary>Boundary for round outlines: straight out from the centre to the inscribed circle.</summary>
    protected static (double X, double Y) RadialBoundary(IBox b, double tx, double ty)
    {
        double cx = b.X + b.W / 2, cy = b.Y + b.H / 2;
        double dx = tx - cx, dy = ty - cy;
        double r = Math.Min(b.W, b.H) / 2;
        double len = Math.Sqrt(dx * dx + dy * dy);
        return (cx + dx / len * r, cy + dy / len * r);
    }

    /// <summary>Badge on the inscribed circle at 45° (circles, seals, octagons).</summary>
    protected static (double X, double Y) RadialBadge(IBox b)
    {
        double r = Math.Min(b.W, b.H) / 2;
        return (b.X + b.W / 2 + r * D45, b.Y + b.H / 2 + r * D45);
    }

    protected static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
}
