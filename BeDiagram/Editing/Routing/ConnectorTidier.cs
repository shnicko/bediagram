using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;

namespace BeDiagram.Editing.Routing;

/// <summary>
/// "Tidy connectors": a one-off clean-up the user runs when they want it (never automatic). For each elbow connector
/// that passes through a shape, a text box or the text drawn under a shape, it tries every pair of sides and a range of
/// positions for the middle segment, using the normal router
/// (<see cref="Geometry.Route(IBox, IBox, string, string?, string?, double)"/>), and keeps the cheapest route:
/// <list type="number">
/// <item>no shape, text box or shape text crossed;</item>
/// <item>then not squeezing past a shape closer than <see cref="Clearance"/>, not reaching its target through the
/// target's text (hiding the arrowhead) or running back through its source's text, not arriving on a side another connector leaves from, not running along another connector and
/// not crossing other connectors;</item>
/// <item>then fewest corners; then shortest.</item>
/// </list>
/// Connectors that meet at the same shape are allowed to share their last stretch and don't count as crossing each
/// other there (lines fanning into one shape do that naturally).
/// The result is stored the same way as a hand-placed route (<see cref="Edge.FromSide"/>, <see cref="Edge.ToSide"/>,
/// <see cref="Edge.Bend"/>), so nothing new is drawn and the file format doesn't change.
/// Connectors that are already clear, and straight connectors, are left exactly as they are.
/// </summary>
public class ConnectorTidier
{
    /// <summary>Gap kept between a connector and a shape it goes round.</summary>
    public const double Clearance = 16;
    /// <summary>A shape is "crossed" when a line comes within this of its outline box.</summary>
    const double Touch = 3;
    /// <summary>Score weights (see the class summary for the order).</summary>
    const double CrossingCost = 10000, NearCost = 400, OwnTextCost = 500, OverlapCost = 300, OverlapPerPx = 2,
                 LineCrossCost = 120, PortClashCost = 350, CornerCost = 30;

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
    /// Works out new routes for <paramref name="edges"/> without changing anything. A connector needs tidying when it
    /// passes through a shape, a text box or the text under a shape, when it runs through its own end shapes' text
    /// (e.g. an arrowhead hidden under a description), or when it arrives on a side another connector leaves from
    /// (or leaves where another arrives). It gets the cheapest route, if that's better than the one it has.
    /// <see cref="Apply"/> stores them.
    /// </summary>
    public TidyPlan Plan(IEnumerable<Edge> edges)
    {
        TidyPlan plan = new TidyPlan();
        // Where every connector runs now, so a new route can keep off the others (updated as routes are planned).
        Dictionary<string, (Edge Edge, EdgeRoute Route)> lines = new Dictionary<string, (Edge, EdgeRoute)>();
        foreach (Edge o in d.Edges)
            if (PanelGeometry.Ends(o, find) is (IBox oa, IBox ob)) lines[o.Id] = (o, Geometry.Route(oa, ob, o));
        foreach (Edge e in edges.Where(e => e.Style == EdgeStyles.Elbow))
        {
            if (PanelGeometry.Ends(e, find) is not (IBox a, IBox b) || e.From == e.To) continue;
            Context ctx = new Context
            {
                A = a, B = b, Obstacles = ObstaclesFor(a, b), OwnText = OwnText(a, b),
                // Connectors sharing an end with this one may share its last stretch, so only the others count for
                // running alongside / crossing; all of them count for which side of a shape is in use.
                Others = lines.Values.Where(l => l.Edge.Id != e.Id && !SharesEnd(l.Edge, e)).Select(l => l.Route.Points).ToList(),
                Ports = lines.Values.Where(l => l.Edge.Id != e.Id).ToList(),
            };
            EdgeRoute now = Geometry.Route(a, b, e);
            Score current = ScoreOf(now, e.Bend, ctx);
            if (current.Hits == 0 && current.OwnText == 0 && current.PortClashes == 0) continue;
            plan.Crossing++;
            if (Best(ctx) is not { } pick || pick.Score.Cost >= current.Cost - 1)
            {
                if (current.Hits == 0) plan.Cleared++; // nothing better, but it never crossed a shape
                continue;
            }
            plan.Routes.Add((e, pick.From, pick.To, pick.Bend));
            lines[e.Id] = (e, Geometry.Route(a, b, EdgeStyles.Elbow, pick.From, pick.To, pick.Bend));
            if (pick.Score.Hits == 0) plan.Cleared++;
        }
        return plan;
    }

    /// <summary>Stores the planned routes the same way as hand-placed ones (sides + bend).</summary>
    public static void Apply(TidyPlan plan)
    {
        foreach ((Edge e, string from, string to, double bend) in plan.Routes) { e.FromSide = from; e.ToSide = to; e.Bend = bend; }
    }

    /// <summary>What a connector's candidate routes are scored against.</summary>
    sealed class Context
    {
        public required IBox A { get; init; }
        public required IBox B { get; init; }
        public required List<Rect> Obstacles { get; init; }
        public required (Rect? From, Rect? To) OwnText { get; init; }
        public required List<List<(double X, double Y)>> Others { get; init; }
        public required List<(Edge Edge, EdgeRoute Route)> Ports { get; init; }
    }

    /// <summary>A route's total cost, and the counts that decide whether a connector needs tidying.</summary>
    readonly record struct Score(double Cost, int Hits, int OwnText, int PortClashes);

    static bool SharesEnd(Edge x, Edge y) => x.From == y.From || x.From == y.To || x.To == y.From || x.To == y.To;

    /// <summary>The cheapest route over every side pair and middle-segment position.</summary>
    (string From, string To, double Bend, Score Score)? Best(Context ctx)
    {
        (string From, string To, double Bend, Score Score)? best = null;
        foreach (string s1 in Sides.All)
            foreach (string s2 in Sides.All)
            {
                EdgeRoute plain = Geometry.Route(ctx.A, ctx.B, EdgeStyles.Elbow, s1, s2, 0);
                foreach (double bend in BendsToTry(plain, ctx.Obstacles))
                {
                    EdgeRoute r = bend == 0 ? plain : Geometry.Route(ctx.A, ctx.B, EdgeStyles.Elbow, s1, s2, bend);
                    Score score = ScoreOf(r, bend, ctx);
                    if (best is null || score.Cost < best.Value.Score.Cost) best = (s1, s2, bend, score);
                }
            }
        return best;
    }

    /// <summary>The cost of one route (see the class summary for what weighs most).</summary>
    static Score ScoreOf(EdgeRoute r, double bend, Context ctx)
    {
        List<(double X, double Y)> pts = r.Points;
        int hits = Crossings(pts, ctx.Obstacles, ctx.A, ctx.B);
        int own = (ctx.OwnText.From is Rect ft && AnySegmentHits(pts, ft, 1, pts.Count - 1) ? 1 : 0)
                  + (ctx.OwnText.To is Rect tt && AnySegmentHits(pts, tt, 0, pts.Count - 1) ? 1 : 0);
        int clashes = PortClashes(r, ctx);
        double shared = SharedLength(pts, ctx.Others);
        double cost = hits * CrossingCost
                      + NearMisses(pts, ctx.Obstacles) * NearCost
                      + own * OwnTextCost
                      + clashes * PortClashCost
                      + (shared > 0 ? OverlapCost + shared * OverlapPerPx : 0)
                      + LineCrossings(pts, ctx.Others) * LineCrossCost
                      + (pts.Count - 2) * CornerCost + Length(pts) + Math.Abs(bend) * 0.1;
        return new Score(cost, hits, own, clashes);
    }

    /// <summary>
    /// Other connectors using the same side of either end the other way round: arriving where one leaves, or leaving
    /// where one arrives (lines going the same way on one side are fine — they fan in or out together).
    /// </summary>
    static int PortClashes(EdgeRoute r, Context ctx)
    {
        int clashes = 0;
        foreach ((Edge o, EdgeRoute or) in ctx.Ports)
        {
            if (r.FromSide is not null && o.To == ctx.A.Id && or.ToSide == r.FromSide) clashes++;
            if (r.ToSide is not null && o.From == ctx.B.Id && or.FromSide == r.ToSide) clashes++;
        }
        return clashes;
    }

    /// <summary>
    /// Middle-segment offsets worth trying: none, a sweep either side, and just clear of each obstacle's edges
    /// (a vertical middle segment moves in x, a horizontal one in y).
    /// </summary>
    static IEnumerable<double> BendsToTry(EdgeRoute r, List<Rect> obstacles)
    {
        yield return 0;
        if (r.BendIndex < 0) yield break;
        for (double step = 20; step <= 240; step += 20) { yield return step; yield return -step; }
        bool vertical = r.BendIsVertical;
        double at = vertical ? r.Points[r.BendIndex].X : r.Points[r.BendIndex].Y;
        foreach (Rect o in obstacles)
        {
            double lo = vertical ? o.X : o.Y, hi = vertical ? o.X + o.W : o.Y + o.H;
            yield return Math.Round(lo - Clearance - at);
            yield return Math.Round(hi + Clearance - at);
        }
    }

    /// <summary>
    /// What a connector between <paramref name="a"/> and <paramref name="b"/> should keep out of: every shape and text
    /// box except the two ends (and what's inside an end that is a group), plus the label or description drawn under
    /// those shapes. Groups themselves aren't obstacles (lines cross lane borders all the time).
    /// </summary>
    List<Rect> ObstaclesFor(IBox a, IBox b)
    {
        HashSet<string> skip = SkipFor(a, b);
        List<Rect> list = new List<Rect>();
        foreach (Node n in d.Nodes.Where(n => !skip.Contains(n.Id)))
        {
            list.Add(new Rect(n.X, n.Y, n.W, n.H));
            if (TextUnder(n) is Rect t) list.Add(t);
        }
        list.AddRange(d.TextBoxes.Where(t => !skip.Contains(t.Id)).Select(t => new Rect(t.X, t.Y, t.W, t.H)));
        return list;
    }

    /// <summary>Text drawn under the connector's own end shapes (source first, then target, or null). A line may leave
    /// down through its source's description (it just runs out from under it), but shouldn't run back through it later,
    /// and shouldn't reach its target through the target's text, which would hide the arrowhead.</summary>
    (Rect? From, Rect? To) OwnText(IBox a, IBox b) =>
        (a is Node na ? TextUnder(na) : null, b is Node nb ? TextUnder(nb) : null);

    HashSet<string> SkipFor(IBox a, IBox b)
    {
        HashSet<string> skip = new HashSet<string> { a.Id, b.Id };
        foreach (IBox end in new[] { a, b })
            if (end is Group g)
                foreach (IGroupable m in tree.Descendants(g)) skip.Add(m.Id);
        return skip;
    }

    /// <summary>The block of text drawn outside a shape (a label under a circle, a description), or null.</summary>
    static Rect? TextUnder(Node n) =>
        Geometry.OutsideTextBox(n, Geometry.Layout(n)) is { } r ? new Rect(r.X, r.Y, r.W, r.H) : null;

    /// <summary>
    /// How many obstacles the route passes through. The route's own ends count too when a middle segment runs back
    /// through them (the first and last segments start on their outlines, so those are fine).
    /// </summary>
    static int Crossings(List<(double X, double Y)> pts, List<Rect> obstacles, IBox a, IBox b)
    {
        int hits = 0;
        foreach (Rect o in obstacles)
            if (AnySegmentHits(pts, o, 0, pts.Count - 1)) hits++;
        if (pts.Count > 3)
        {
            if (AnySegmentHits(pts, new Rect(a.X, a.Y, a.W, a.H), 1, pts.Count - 1)) hits++;
            if (AnySegmentHits(pts, new Rect(b.X, b.Y, b.W, b.H), 0, pts.Count - 2)) hits++;
        }
        return hits;
    }

    /// <summary>Obstacles the route doesn't cross but passes closer to than <see cref="Clearance"/> (squeezing through a gap).
    /// The first and last segments are left out: they have to start and end right next to shapes.</summary>
    static int NearMisses(List<(double X, double Y)> pts, List<Rect> obstacles)
    {
        int near = 0;
        foreach (Rect o in obstacles)
        {
            Rect wide = new Rect(o.X - (Clearance - 1), o.Y - (Clearance - 1), o.W + 2 * (Clearance - 1), o.H + 2 * (Clearance - 1));
            bool close = false;
            for (int i = 1; i + 2 < pts.Count && !close; i++) close = SegmentHitsRect(pts[i], pts[i + 1], wide);
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

    /// <summary>How many times the route crosses other connectors (one horizontal and one vertical segment meeting
    /// strictly inside both).</summary>
    static int LineCrossings(List<(double X, double Y)> pts, List<List<(double X, double Y)>> others)
    {
        int count = 0;
        for (int i = 0; i + 1 < pts.Count; i++)
            foreach (List<(double X, double Y)> o in others)
                for (int j = 0; j + 1 < o.Count; j++)
                    if (Cross(pts[i], pts[i + 1], o[j], o[j + 1])) count++;
        return count;
    }

    static bool Cross((double X, double Y) p, (double X, double Y) q, (double X, double Y) r, (double X, double Y) s)
    {
        bool pqH = Math.Abs(p.Y - q.Y) < 0.5, pqV = Math.Abs(p.X - q.X) < 0.5;
        bool rsH = Math.Abs(r.Y - s.Y) < 0.5, rsV = Math.Abs(r.X - s.X) < 0.5;
        if (pqH && rsV) return Between(r.X, p.X, q.X) && Between(p.Y, r.Y, s.Y);
        if (pqV && rsH) return Between(p.X, r.X, s.X) && Between(r.Y, p.Y, q.Y);
        return false;
    }

    /// <summary>True when <paramref name="v"/> lies strictly between <paramref name="a"/> and <paramref name="b"/> (2 px in).</summary>
    static bool Between(double v, double a, double b) => v > Math.Min(a, b) + 2 && v < Math.Max(a, b) - 2;

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
    static bool AnySegmentHits(List<(double X, double Y)> pts, Rect o, int from, int to)
    {
        Rect grown = new Rect(o.X - Touch, o.Y - Touch, o.W + 2 * Touch, o.H + 2 * Touch);
        for (int i = from; i < to; i++)
            if (SegmentHitsRect(pts[i], pts[i + 1], grown)) return true;
        return false;
    }

    /// <summary>Segment against an axis-aligned rectangle (Liang–Barsky clipping); touching only at an end point doesn't count.</summary>
    static bool SegmentHitsRect((double X, double Y) p, (double X, double Y) q, Rect box)
    {
        double l = box.X, t = box.Y, r = box.X + box.W, b = box.Y + box.H;
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
