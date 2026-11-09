using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;

namespace BeDiagram.Models.Routing;

/// <summary>
/// Right-angled connectors. With no sides pinned it uses the automatic sides (<see cref="EdgeRouter.AutoSides"/>) and
/// the original layout; with both pinned it routes between them; with one pinned it tries each side for the other end
/// and keeps the shortest, simplest route.
/// </summary>
public class ElbowRouting : IConnectorRouting
{
    /// <summary>How far a route leaves a side before it turns.</summary>
    const double Stub = 20;

    public EdgeStyle Style => EdgeStyle.Elbow;

    public EdgeRoute Route(IBox a, IBox b, Side? fromSide, Side? toSide, double bend)
    {
        if (fromSide is null && toSide is null)
        {
            (Side s1, Side s2) = EdgeRouter.AutoSides(a, b);
            return ElbowRoute(a, b, s1, s2, bend, legacy: true);
        }
        if (fromSide is { } pinFrom && toSide is { } pinTo) return ElbowRoute(a, b, pinFrom, pinTo, bend, legacy: false);

        // One end pinned: pick the other end's side that gives the shortest, simplest route.
        EdgeRoute? best = null;
        double bestScore = double.MaxValue;
        foreach (Side s in Sides.All)
        {
            EdgeRoute r = ElbowRoute(a, b, fromSide ?? s, toSide ?? s, bend, legacy: false);
            double score = Length(r.Points) + 25 * r.Points.Count;
            if (score < bestScore) { bestScore = score; best = r; }
        }
        return best!;
    }

    /// <summary>
    /// Right-angled connector from a side of <paramref name="a"/> to a side of <paramref name="b"/>.
    /// Shapes: Z (facing sides), U (same side), L (perpendicular, corner ahead of both), or a 4–5 segment
    /// detour when the target is behind a side. <paramref name="legacy"/> keeps the original automatic
    /// behaviour (always a Z between facing sides, even when the shapes overlap).
    /// </summary>
    EdgeRoute ElbowRoute(IBox a, IBox b, Side s1, Side s2, double bend, bool legacy)
    {
        a = EdgeRouter.ConnectBox(a);
        b = EdgeRouter.ConnectBox(b);
        (double X, double Y) p1 = EdgeRouter.SidePoint(a, s1);
        (double X, double Y) p2 = EdgeRouter.SidePoint(b, s2);
        (double X, double Y) d1 = Sides.Dir(s1);
        (double X, double Y) d2 = Sides.Dir(s2);
        List<(double X, double Y)> pts;
        int idx;

        double Dot((double X, double Y) u, (double X, double Y) v) => u.X * v.X + u.Y * v.Y;
        (double X, double Y) Add((double X, double Y) p, (double X, double Y) d, double k) => (p.X + d.X * k, p.Y + d.Y * k);
        (double X, double Y) Sub((double X, double Y) p, (double X, double Y) q) => (p.X - q.X, p.Y - q.Y);
        // Extent of a box along a unit axis.
        (double Min, double Max) Span(IBox box, (double X, double Y) ax)
        {
            double v1 = Dot((box.X, box.Y), ax), v2 = Dot((box.X + box.W, box.Y + box.H), ax);
            return (Math.Min(v1, v2), Math.Max(v1, v2));
        }

        bool opposite = d1.X == -d2.X && d1.Y == -d2.Y;
        bool same = d1 == d2;

        if (opposite)
        {
            double gap = Dot(Sub(p2, p1), d1);
            if (gap > 0 || legacy)
            {
                // Z: out, across at half way, in.
                (double X, double Y) m1 = Add(p1, d1, gap / 2);
                (double X, double Y) m2 = Add(p2, d2, gap / 2);
                pts = new() { p1, m1, m2, p2 };
                idx = 1;
            }
            else
            {
                // Target is behind the start side: step out, cross over between (or around) the shapes, step back in.
                (double X, double Y) n = (Math.Abs(d1.Y), Math.Abs(d1.X)); // perpendicular axis
                (double aMin, double aMax) = Span(a, n);
                (double bMin, double bMax) = Span(b, n);
                double lane = aMax < bMin ? (aMax + bMin) / 2
                            : bMax < aMin ? (bMax + aMin) / 2
                            : Math.Max(aMax, bMax) + Stub;
                (double X, double Y) q1 = Add(p1, d1, Stub);
                (double X, double Y) q2 = Add(p2, d2, Stub);
                double o1 = lane - Dot(q1, n), o2 = lane - Dot(q2, n);
                pts = new() { p1, q1, Add(q1, n, o1), Add(q2, n, o2), q2, p2 };
                idx = 2;
            }
        }
        else if (same)
        {
            // U: out past whichever end sticks out further, across, back in.
            double level = Math.Max(Dot(p1, d1), Dot(p2, d1)) + Stub;
            (double X, double Y) q1 = Add(p1, d1, level - Dot(p1, d1));
            (double X, double Y) q2 = Add(p2, d2, level - Dot(p2, d2));
            pts = new() { p1, q1, q2, p2 };
            idx = 1;
        }
        else
        {
            double t1 = Dot(Sub(p2, p1), d1); // how far ahead of the start side the end point is
            double t2 = Dot(Sub(p1, p2), d2); // how far ahead of the end side the start point is
            if (t1 > Stub / 2 && t2 > Stub / 2)
            {
                // L: a single corner.
                pts = new() { p1, Add(p1, d1, t1), p2 };
                idx = -1;
            }
            else
            {
                // Step out of the start side (half way to the target's box when there's room), run across, step into the end side.
                double ahead = Span(b, d1).Min - Dot(p1, d1); // gap to the target box's near face
                double out1 = ahead > 2 * Stub ? ahead / 2 : Stub;
                (double X, double Y) q1 = Add(p1, d1, out1);
                (double X, double Y) q2 = Add(p2, d2, Stub);
                (double X, double Y) x = Add(q1, d2, Dot(Sub(q2, q1), d2));
                pts = new() { p1, q1, x, q2, p2 };
                idx = 1;
            }
        }

        if (idx >= 0 && bend != 0)
        {
            bool vertical = Math.Abs(pts[idx].X - pts[idx + 1].X) < 0.5;
            for (int k = idx; k <= idx + 1; k++)
                pts[k] = vertical ? (pts[k].X + bend, pts[k].Y) : (pts[k].X, pts[k].Y + bend);
        }
        else if (idx == 1 && pts.Count == 4 && bend == 0
                 && Math.Abs(pts[1].X - pts[2].X) < 1 && Math.Abs(pts[1].Y - pts[2].Y) < 1)
        {
            // Ends line up: one straight run, as before.
            pts = new() { p1, p2 };
            idx = -1;
        }
        return new EdgeRoute(pts, idx, s1, s2);
    }

    static double Length(List<(double X, double Y)> pts)
    {
        double len = 0;
        for (int i = 1; i < pts.Count; i++) len += Math.Abs(pts[i].X - pts[i - 1].X) + Math.Abs(pts[i].Y - pts[i - 1].Y);
        return len;
    }
}
