namespace BeDiagram.Models.Heads;

/// <summary>Where the quick-change spot for a connector's head sits: a little way in from that end, along the line,
/// so it clears the head itself and the ● end handle.</summary>
public static class HeadSpots
{
    /// <summary>How far along the line from the end the spot sits, in screen pixels.</summary>
    public const double Distance = 30;

    /// <summary>The spot near the start (<paramref name="atStart"/>) or the end of the route
    /// <paramref name="points"/>: <see cref="Distance"/> screen pixels along the line at <paramref name="zoom"/>, or a
    /// third of a short line.</summary>
    public static (double X, double Y) Near(List<(double X, double Y)> points, bool atStart, double zoom)
    {
        if (points.Count == 0)
        {
            return (0, 0);
        }
        // Walk from the chosen end.
        List<(double X, double Y)> walk = new List<(double X, double Y)>(points);
        if (!atStart)
        {
            walk.Reverse();
        }
        double total = 0;
        for (int i = 1; i < walk.Count; i++)
        {
            total += Length(walk[i - 1], walk[i]);
        }
        double want = Math.Min(Distance / zoom, total / 3);
        for (int i = 1; i < walk.Count; i++)
        {
            double seg = Length(walk[i - 1], walk[i]);
            if (seg >= want && seg > 0)
            {
                double t = want / seg;
                return (walk[i - 1].X + (walk[i].X - walk[i - 1].X) * t, walk[i - 1].Y + (walk[i].Y - walk[i - 1].Y) * t);
            }
            want -= seg;
        }
        return walk[walk.Count - 1];
    }

    static double Length((double X, double Y) a, (double X, double Y) b)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }
}
