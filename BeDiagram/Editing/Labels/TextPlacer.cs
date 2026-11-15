using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Routing;
using BeDiagram.Models.Text;

namespace BeDiagram.Editing.Labels;

/// <summary>
/// "Place text clear of connectors": a one-off clean-up the user runs (never automatic, like Tidy connectors). For each
/// shape with text outside it (a label below a circle or outcome, a description) it tries the text underneath, above,
/// to the right and to the left (<see cref="Node.LabelSide"/>) and keeps the side where the text:
/// <list type="number">
/// <item>is crossed by the fewest connectors;</item>
/// <item>then overlaps the fewest other shapes, text boxes and shape text;</item>
/// <item>then is earliest in the order underneath, above, right, left (so a clear label stays underneath).</item>
/// </list>
/// The side it already has wins a tie, so running it twice changes nothing. Text dragged free
/// (<see cref="Node.FreeLabel"/>) is left where the user put it. Only <see cref="Node.LabelSide"/> changes, so the
/// renderers and the file format need nothing new.
/// </summary>
public class TextPlacer
{
    /// <summary>The sides tried, in order of preference.</summary>
    static readonly Side[] Order = { Side.Bottom, Side.Top, Side.Right, Side.Left };
    /// <summary>Score weights: a connector through the text matters most.</summary>
    const double LineCost = 1000, OverlapCost = 100, OrderCost = 1;
    /// <summary>A line or box this close to the text block counts as touching it.</summary>
    const double Touch = 2;

    readonly Diagram d;
    readonly Func<string, IBox?> find;

    public TextPlacer(Diagram diagram, Func<string, IBox?> findBox)
    {
        d = diagram;
        find = findBox;
    }

    /// <summary>
    /// Moves the text of <paramref name="nodes"/> to its clearest side, one shape after another (so a shape's text keeps
    /// clear of text already placed). Returns how many shapes' text moved.
    /// </summary>
    public int Apply(List<Node> nodes)
    {
        List<List<(double X, double Y)>> lines = Routes();
        int moved = 0;
        foreach (Node n in nodes)
        {
            if (n.FreeLabel is not null || !TextLayout.HasOutsideText(n))
            {
                continue;
            }
            Side? before = n.LabelSide;
            Side? best = Best(n, lines);
            n.LabelSide = best;
            if (before != best)
            {
                moved++;
            }
        }
        return moved;
    }

    /// <summary>Where every connector runs.</summary>
    List<List<(double X, double Y)>> Routes()
    {
        List<List<(double X, double Y)>> lines = new List<List<(double X, double Y)>>();
        foreach (Edge e in d.Edges)
        {
            (IBox A, IBox B)? ends = e.Ends(find);
            if (ends is null)
            {
                continue;
            }
            lines.Add(EdgeRouter.Route(ends.Value.A, ends.Value.B, e).Points);
        }
        return lines;
    }

    /// <summary>The cheapest side for a shape's text (null = underneath, which is how the file stores it).</summary>
    Side? Best(Node n, List<List<(double X, double Y)>> lines)
    {
        Side? original = n.LabelSide;
        List<Rect> obstacles = ObstaclesFor(n);
        Side? best = original;
        double bestCost = double.MaxValue;
        for (int i = 0; i < Order.Length; i++)
        {
            Side? candidate = Order[i];
            if (candidate == Side.Bottom)
            {
                candidate = null;
            }
            n.LabelSide = candidate;
            Rect? text = TextBlock(n);
            if (text is null)
            {
                continue;
            }
            double cost = Crossings(text, lines) * LineCost + Overlaps(text, obstacles) * OverlapCost + i * OrderCost;
            // The side the text already has keeps it on a tie (the order cost is the only difference).
            if (candidate == original)
            {
                cost = cost - OrderCost * Order.Length;
            }
            if (cost < bestCost)
            {
                bestCost = cost;
                best = candidate;
            }
        }
        n.LabelSide = original;
        return best;
    }

    /// <summary>The block of text outside a shape, where its current settings put it.</summary>
    static Rect? TextBlock(Node n)
    {
        (double X, double Y, double W, double H)? r = TextLayout.OutsideTextBox(n, TextLayout.Layout(n));
        if (r is null)
        {
            return null;
        }
        return new Rect(r.Value.X, r.Value.Y, r.Value.W, r.Value.H);
    }

    /// <summary>What the text should keep off: the other shapes and their text, and the text boxes.</summary>
    List<Rect> ObstaclesFor(Node n)
    {
        List<Rect> list = new List<Rect>();
        foreach (Node o in d.Nodes)
        {
            if (o.Id == n.Id)
            {
                continue;
            }
            list.Add(new Rect(o.X, o.Y, o.W, o.H));
            Rect? text = TextBlock(o);
            if (text is not null)
            {
                list.Add(text);
            }
        }
        foreach (TextBox t in d.TextBoxes)
        {
            list.Add(new Rect(t.X, t.Y, t.W, t.H));
        }
        return list;
    }

    /// <summary>How many connectors pass through the text block.</summary>
    static int Crossings(Rect text, List<List<(double X, double Y)>> lines)
    {
        Rect wide = new Rect(text.X - Touch, text.Y - Touch, text.W + 2 * Touch, text.H + 2 * Touch);
        int count = 0;
        foreach (List<(double X, double Y)> pts in lines)
        {
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                if (SegmentHits(pts[i], pts[i + 1], wide))
                {
                    count++;
                    break;
                }
            }
        }
        return count;
    }

    /// <summary>How many obstacles the text block overlaps.</summary>
    static int Overlaps(Rect text, List<Rect> obstacles)
    {
        int count = 0;
        foreach (Rect o in obstacles)
        {
            bool overlapX = text.X < o.X + o.W + Touch && text.X + text.W > o.X - Touch;
            bool overlapY = text.Y < o.Y + o.H + Touch && text.Y + text.H > o.Y - Touch;
            if (overlapX && overlapY)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// True when the segment from <paramref name="a"/> to <paramref name="b"/> passes through the rectangle, at any angle
    /// (straight connectors are diagonal). Clips the segment against each pair of sides in turn (Liang–Barsky).
    /// </summary>
    static bool SegmentHits((double X, double Y) a, (double X, double Y) b, Rect r)
    {
        double dx = b.X - a.X;
        double dy = b.Y - a.Y;
        double t0 = 0;
        double t1 = 1;
        double[] p = { -dx, dx, -dy, dy };
        double[] q = { a.X - r.X, r.X + r.W - a.X, a.Y - r.Y, r.Y + r.H - a.Y };
        for (int i = 0; i < 4; i++)
        {
            if (Math.Abs(p[i]) < 1e-9)
            {
                // Parallel to these two sides: inside the band or not at all.
                if (q[i] < 0)
                {
                    return false;
                }
                continue;
            }
            double t = q[i] / p[i];
            if (p[i] < 0)
            {
                t0 = Math.Max(t0, t);
            }
            else
            {
                t1 = Math.Min(t1, t);
            }
            if (t0 > t1)
            {
                return false;
            }
        }
        return true;
    }
}
