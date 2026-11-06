using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;

namespace BeDiagram.Editing.Routing;

/// <summary>
/// "Tidy connectors": a one-off clean-up the user runs when they want it (never automatic). For each elbow connector
/// that passes through a shape or text box, it tries every pair of sides and a range of positions for the middle
/// segment, using the normal router (<see cref="Geometry.Route(IBox, IBox, string, string?, string?, double)"/>),
/// and keeps the clearest route (no shape crossed; then no squeezing through narrow gaps or running along another
/// connector; then fewest corners; then shortest). The result is stored the same way as a hand-placed route (<see cref="Edge.FromSide"/>,
/// <see cref="Edge.ToSide"/>, <see cref="Edge.Bend"/>), so nothing new is drawn and the file format doesn't change.
/// Connectors that are already clear, and straight connectors, are left exactly as they are.
/// </summary>
public class ConnectorTidier
{
    /// <summary>Gap kept between a connector and a shape it goes round.</summary>
    public const double Clearance = 16;
    /// <summary>A shape is "crossed" when a line comes within this of its outline box.</summary>
    const double Touch = 3;
    /// <summary>Score weights: any crossing outweighs everything else; then squeezing past a shape closer than
    /// <see cref="Clearance"/>, or running along another connector; then fewer corners; then shorter.</summary>
    const double CrossingCost = 10000, NearCost = 400, OverlapCost = 300, OverlapPerPx = 2, CornerCost = 30;

    readonly Diagram d;
    readonly GroupTree tree;
    readonly Func<string, IBox?> find;

    public ConnectorTidier(Diagram diagram, Func<string, IBox?> findBox)
    {
        d = diagram;
        tree = new GroupTree(diagram);
        find = findBox;
    }

    /// <summary>
    /// Works out new routes for <paramref name="edges"/> without changing anything: the connectors that pass through a
    /// shape, each with a better route where there is one. <see cref="Apply"/> stores them.
    /// </summary>
    public TidyPlan Plan(IEnumerable<Edge> edges)
    {
        TidyPlan plan = new TidyPlan();
        // Where every connector runs now, so a new route can keep off the others (updated as routes are planned).
        Dictionary<string, List<(double X, double Y)>> lines = new Dictionary<string, List<(double X, double Y)>>();
        foreach (Edge o in d.Edges)
            if (find(o.From) is IBox oa && find(o.To) is IBox ob) lines[o.Id] = Geometry.Route(oa, ob, o).Points;
        foreach (Edge e in edges.Where(e => e.Style == EdgeStyles.Elbow))
        {
            if (find(e.From) is not IBox a || find(e.To) is not IBox b || ReferenceEquals(a, b)) continue;
            List<IBox> obstacles = ObstaclesFor(a, b);
            int hits = Crossings(Geometry.Route(a, b, e).Points, obstacles, a, b);
            if (hits == 0) continue;
            plan.Crossing++;
            List<List<(double X, double Y)>> others = lines.Where(kv => kv.Key != e.Id).Select(kv => kv.Value).ToList();
            if (Best(a, b, obstacles, others) is not { } pick || pick.Hits >= hits) continue;
            plan.Routes.Add((e, pick.From, pick.To, pick.Bend));
            lines[e.Id] = Geometry.Route(a, b, EdgeStyles.Elbow, pick.From, pick.To, pick.Bend).Points;
            if (pick.Hits == 0) plan.Cleared++;
        }
        return plan;
    }

    /// <summary>Stores the planned routes the same way as hand-placed ones (sides + bend).</summary>
    public static void Apply(TidyPlan plan)
    {
        foreach ((Edge e, string from, string to, double bend) in plan.Routes) { e.FromSide = from; e.ToSide = to; e.Bend = bend; }
    }

    /// <summary>The cheapest route over every side pair and middle-segment position.</summary>
    (string From, string To, double Bend, int Hits)? Best(IBox a, IBox b, List<IBox> obstacles, List<List<(double X, double Y)>> others)
    {
        (string From, string To, double Bend, int Hits)? best = null;
        double bestCost = double.MaxValue;
        foreach (string s1 in Sides.All)
            foreach (string s2 in Sides.All)
            {
                EdgeRoute plain = Geometry.Route(a, b, EdgeStyles.Elbow, s1, s2, 0);
                foreach (double bend in BendsToTry(plain, obstacles))
                {
                    EdgeRoute r = bend == 0 ? plain : Geometry.Route(a, b, EdgeStyles.Elbow, s1, s2, bend);
                    int hits = Crossings(r.Points, obstacles, a, b);
                    double shared = SharedLength(r.Points, others);
                    double cost = hits * CrossingCost
                                  + NearMisses(r.Points, obstacles) * NearCost
                                  + (shared > 0 ? OverlapCost + shared * OverlapPerPx : 0)
                                  + (r.Points.Count - 2) * CornerCost + Length(r.Points) + Math.Abs(bend) * 0.1;
                    if (cost < bestCost) { bestCost = cost; best = (s1, s2, bend, hits); }
                }
            }
        return best;
    }

    /// <summary>
    /// Middle-segment offsets worth trying: none, a sweep either side, and just clear of each obstacle's edges
    /// (a vertical middle segment moves in x, a horizontal one in y).
    /// </summary>
    static IEnumerable<double> BendsToTry(EdgeRoute r, List<IBox> obstacles)
    {
        yield return 0;
        if (r.BendIndex < 0) yield break;
        for (double step = 20; step <= 240; step += 20) { yield return step; yield return -step; }
        bool vertical = r.BendIsVertical;
        double at = vertical ? r.Points[r.BendIndex].X : r.Points[r.BendIndex].Y;
        foreach (IBox o in obstacles)
        {
            double lo = vertical ? o.X : o.Y, hi = vertical ? o.X + o.W : o.Y + o.H;
            yield return Math.Round(lo - Clearance - at);
            yield return Math.Round(hi + Clearance - at);
        }
    }

    /// <summary>Shapes and text boxes a connector between <paramref name="a"/> and <paramref name="b"/> should keep out of:
    /// everything except the two ends and what's inside an end that is a group. Groups themselves aren't obstacles
    /// (lines cross lane borders all the time).</summary>
    List<IBox> ObstaclesFor(IBox a, IBox b)
    {
        HashSet<string> skip = new HashSet<string> { a.Id, b.Id };
        foreach (IBox end in new[] { a, b })
            if (end is Group g)
                foreach (IGroupable m in tree.Descendants(g)) skip.Add(m.Id);
        List<IBox> list = new List<IBox>();
        list.AddRange(d.Nodes.Where(n => !skip.Contains(n.Id)));
        list.AddRange(d.TextBoxes.Where(t => !skip.Contains(t.Id)));
        return list;
    }

    /// <summary>
    /// How many shapes the route passes through. The route's own ends count too when a middle segment runs back
    /// through them (the first and last segments start on their outlines, so those are fine).
    /// </summary>
    static int Crossings(List<(double X, double Y)> pts, List<IBox> obstacles, IBox a, IBox b)
    {
        int hits = 0;
        foreach (IBox o in obstacles)
            if (AnySegmentHits(pts, o, 0, pts.Count - 1)) hits++;
        if (pts.Count > 3)
        {
            if (AnySegmentHits(pts, a, 1, pts.Count - 1)) hits++;
            if (AnySegmentHits(pts, b, 0, pts.Count - 2)) hits++;
        }
        return hits;
    }

    /// <summary>Shapes the route doesn't cross but passes closer to than <see cref="Clearance"/> (squeezing through a gap).
    /// The first and last segments are left out: they have to start and end right next to shapes.</summary>
    static int NearMisses(List<(double X, double Y)> pts, List<IBox> obstacles)
    {
        int near = 0;
        foreach (IBox o in obstacles)
        {
            double l = o.X - Clearance + 1, t = o.Y - Clearance + 1, r = o.X + o.W + Clearance - 1, b = o.Y + o.H + Clearance - 1;
            bool close = false;
            for (int i = 1; i + 2 < pts.Count && !close; i++) close = SegmentHitsRect(pts[i], pts[i + 1], l, t, r, b);
            if (close && !AnySegmentHits(pts, o, 0, pts.Count - 1)) near++;
        }
        return near;
    }

    /// <summary>Length (px) of the route that runs on top of other connectors (same line, overlapping).</summary>
    static double SharedLength(List<(double X, double Y)> pts, List<List<(double X, double Y)>> others)
    {
        double shared = 0;
        for (int i = 0; i + 1 < pts.Count; i++)
            foreach (List<(double X, double Y)> o in others)
                for (int j = 0; j + 1 < o.Count; j++)
                    shared += Overlap(pts[i], pts[i + 1], o[j], o[j + 1]);
        return shared;
    }

    /// <summary>Overlap of two axis-aligned segments lying on the same line (within 2 px), else 0.</summary>
    static double Overlap((double X, double Y) p, (double X, double Y) q, (double X, double Y) r, (double X, double Y) s)
    {
        bool pqH = Math.Abs(p.Y - q.Y) < 0.5, rsH = Math.Abs(r.Y - s.Y) < 0.5;
        bool pqV = Math.Abs(p.X - q.X) < 0.5, rsV = Math.Abs(r.X - s.X) < 0.5;
        if (pqH && rsH && Math.Abs(p.Y - r.Y) < 2)
            return Math.Max(0, Math.Min(Math.Max(p.X, q.X), Math.Max(r.X, s.X)) - Math.Max(Math.Min(p.X, q.X), Math.Min(r.X, s.X)));
        if (pqV && rsV && Math.Abs(p.X - r.X) < 2)
            return Math.Max(0, Math.Min(Math.Max(p.Y, q.Y), Math.Max(r.Y, s.Y)) - Math.Max(Math.Min(p.Y, q.Y), Math.Min(r.Y, s.Y)));
        return 0;
    }

    /// <summary>True when any segment from index <paramref name="from"/> up to (not including) <paramref name="to"/> enters the box.</summary>
    static bool AnySegmentHits(List<(double X, double Y)> pts, IBox o, int from, int to)
    {
        double l = o.X - Touch, t = o.Y - Touch, r = o.X + o.W + Touch, btm = o.Y + o.H + Touch;
        for (int i = from; i < to; i++)
            if (SegmentHitsRect(pts[i], pts[i + 1], l, t, r, btm)) return true;
        return false;
    }

    /// <summary>Segment against an axis-aligned rectangle (Liang–Barsky clipping); touching only at an end point doesn't count.</summary>
    static bool SegmentHitsRect((double X, double Y) p, (double X, double Y) q, double l, double t, double r, double b)
    {
        double dx = q.X - p.X, dy = q.Y - p.Y;
        double t0 = 0, t1 = 1;
        double[] pp = { -dx, dx, -dy, dy };
        double[] qq = { p.X - l, r - p.X, p.Y - t, b - p.Y };
        for (int i = 0; i < 4; i++)
        {
            if (Math.Abs(pp[i]) < 1e-9) { if (qq[i] < 0) return false; continue; }
            double u = qq[i] / pp[i];
            if (pp[i] < 0) { if (u > t1) return false; if (u > t0) t0 = u; }
            else { if (u < t0) return false; if (u < t1) t1 = u; }
        }
        return t1 - t0 > 1e-6;
    }

    static double Length(List<(double X, double Y)> pts)
    {
        double sum = 0;
        for (int i = 0; i + 1 < pts.Count; i++) sum += Math.Abs(pts[i + 1].X - pts[i].X) + Math.Abs(pts[i + 1].Y - pts[i].Y);
        return sum;
    }
}
