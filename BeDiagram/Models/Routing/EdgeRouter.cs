using System.Text;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Shapes;
using BeDiagram.Models.Styles;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Models.Routing;

/// <summary>Connector routes between two boxes: picks the routing for the connector's style (see IConnectorRouting), and turns a route into path data and a label position. Shared by the canvas, the exports, gestures and Tidy.</summary>
public static class EdgeRouter
{

    /// <summary>Radius of the rounded bends on elbow connectors.</summary>
    public const double BendRadius = 6;


    /// <summary>Full connector geometry: path plus where (and at what angle) its label sits.</summary>
    public static EdgeGeom EdgeGeometry(IBox a, IBox b, EdgeStyle style, LabelOrient labelOrient = LabelOrient.Horizontal, double labelPos = 0.5)
    {
        List<(double X, double Y)> pts = Route(a, b, style);
        (double x, double y, double seg) = PointAt(pts, labelPos);
        return new EdgeGeom(RoundedPathD(pts), x, y, LabelAngle(labelOrient, seg), seg);
    }

    /// <summary>Connector geometry for a stored edge, honouring pinned sides and a dragged bend.</summary>
    public static EdgeGeom EdgeGeometry(IBox a, IBox b, Edge e)
    {
        EdgeRoute r = Route(a, b, e);
        (double x, double y, double seg) = PointAt(r.Points, e.LabelPos);
        return new EdgeGeom(RoundedPathD(r.Points), x, y, LabelAngle(e.LabelOrient, seg), seg);
    }

    public static EdgeRoute Route(IBox a, IBox b, Edge e) =>
        Route(a, b, e.Style, e.FromSide, e.ToSide, e.Bend);

    public static List<(double X, double Y)> Route(IBox a, IBox b, EdgeStyle style) => Route(a, b, style, null, null, 0).Points;

    static readonly IConnectorRouting Elbow = new ElbowRouting();
    static readonly IConnectorRouting Straight = new StraightRouting();

    /// <summary>The routing strategy for a connector style.</summary>
    public static IConnectorRouting RoutingFor(EdgeStyle style) => style == EdgeStyle.Straight ? Straight : Elbow;

    public static EdgeRoute Route(IBox a, IBox b, EdgeStyle style, Side? fromSide, Side? toSide, double bend)
    {
        // A row of a list / IPO node only has ports on its left and right.
        if (a is ItemBox ra) fromSide = ra.SideFacing(b, fromSide);
        if (b is ItemBox rb) toSide = rb.SideFacing(a, toSide);
        return RoutingFor(style).Route(a, b, fromSide, toSide, bend);
    }

    /// <summary>Middle of a side of the shape's box — the same spots as the ● ports.</summary>
    public static (double X, double Y) SidePoint(IBox b, Side side) => side switch
    {
        Side.Top => (b.X + b.W / 2, b.Y),
        Side.Right => (b.X + b.W, b.Y + b.H / 2),
        Side.Bottom => (b.X + b.W / 2, b.Y + b.H),
        _ => (b.X, b.Y + b.H / 2),
    };

    /// <summary>The sides the automatic elbow uses: vertical flow if the target is clearly above/below, otherwise horizontal.</summary>
    public static (Side From, Side To) AutoSides(IBox a, IBox b)
    {
        double dx = (b.X + b.W / 2) - (a.X + a.W / 2), dy = (b.Y + b.H / 2) - (a.Y + a.H / 2);
        bool vertical = Math.Abs(dy) - (a.H + b.H) / 2 > Math.Abs(dx) - (a.W + b.W) / 2 - 20
                        && Math.Abs(dy) > (a.H + b.H) / 2;
        return vertical
            ? (dy > 0 ? (Side.Bottom, Side.Top) : (Side.Top, Side.Bottom))
            : (dx > 0 ? (Side.Right, Side.Left) : (Side.Left, Side.Right));
    }

    /// <summary><see cref="Boundary"/> on the box connectors attach to (out to a major step's halo ring), keeping the shape's outline.</summary>
    internal static (double X, double Y) ConnectBoundary(IBox box, double tx, double ty)
    {
        double cx = box.X + box.W / 2, cy = box.Y + box.H / 2;
        if (Math.Abs(tx - cx) < 1e-6 && Math.Abs(ty - cy) < 1e-6) return (cx, cy);
        return ShapeOutlines.Of(box).Boundary(ConnectBox(box), tx, ty);
    }

    /// <summary>The box connectors attach to: the box itself, or for a major step the box out to its halo ring.</summary>
    internal static IBox ConnectBox(IBox b)
    {
        double h = Boxes.HaloOut(b);
        return h == 0 ? b : new AreaBox(b.X - h, b.Y - h, b.W + 2 * h, b.H + 2 * h);
    }

    /// <summary>A connector polyline with each bend rounded by a small curve (<see cref="BendRadius"/>, less on short
    /// segments). A straight line has no bends, so it comes out the same as <see cref="PathD"/>.</summary>
    public static string RoundedPathD(List<(double X, double Y)> pts)
    {
        if (pts.Count < 3) return Paths.PathD(pts);
        StringBuilder sb = new StringBuilder($"M{F(pts[0].X)},{F(pts[0].Y)}");
        for (int i = 1; i < pts.Count - 1; i++)
        {
            (double X, double Y) prev = pts[i - 1], cur = pts[i], next = pts[i + 1];
            double inLen = Math.Sqrt(Math.Pow(cur.X - prev.X, 2) + Math.Pow(cur.Y - prev.Y, 2));
            double outLen = Math.Sqrt(Math.Pow(next.X - cur.X, 2) + Math.Pow(next.Y - cur.Y, 2));
            double r = Math.Min(BendRadius, Math.Min(inLen, outLen) / 2);
            if (r < 0.5)
            {
                sb.Append($" L{F(cur.X)},{F(cur.Y)}");
                continue;
            }
            (double X, double Y) a = (cur.X - (cur.X - prev.X) / inLen * r, cur.Y - (cur.Y - prev.Y) / inLen * r);
            (double X, double Y) b = (cur.X + (next.X - cur.X) / outLen * r, cur.Y + (next.Y - cur.Y) / outLen * r);
            sb.Append($" L{F(a.X)},{F(a.Y)} Q{F(cur.X)},{F(cur.Y)} {F(b.X)},{F(b.Y)}");
        }
        (double X, double Y) last = pts[^1];
        return sb.Append($" L{F(last.X)},{F(last.Y)}").ToString();
    }

    /// <summary>Point at fraction t of the polyline's length, plus that segment's angle in degrees.</summary>
    public static (double X, double Y, double Angle) PointAt(List<(double X, double Y)> pts, double t)
    {
        t = Math.Clamp(t, 0, 1);
        List<double> lengths = new List<double>();
        for (int i = 0; i < pts.Count - 1; i++)
            lengths.Add(Math.Sqrt(Math.Pow(pts[i + 1].X - pts[i].X, 2) + Math.Pow(pts[i + 1].Y - pts[i].Y, 2)));
        double total = lengths.Sum();
        if (total < 1e-6) return (pts[0].X, pts[0].Y, 0);
        double target = total * t;
        for (int i = 0; i < lengths.Count; i++)
        {
            if (target <= lengths[i] || i == lengths.Count - 1)
            {
                double f = lengths[i] < 1e-6 ? 0 : Math.Min(1, target / lengths[i]);
                (double ax, double ay) = pts[i]; (double bx, double by) = pts[i + 1];
                return (ax + (bx - ax) * f, ay + (by - ay) * f, Math.Atan2(by - ay, bx - ax) * 180 / Math.PI);
            }
            target -= lengths[i];
        }
        return (pts[^1].X, pts[^1].Y, 0);
    }

    /// <summary>Label rotation for an orientation, kept readable (never upside down).</summary>
    public static double LabelAngle(LabelOrient orient, double segmentAngle)
    {
        double a = orient switch
        {
            LabelOrient.Along => segmentAngle,
            LabelOrient.Perpendicular => segmentAngle + 90,
            _ => 0,
        };
        while (a > 90) a -= 180;
        while (a <= -90) a += 180;
        return Math.Round(a, 2);
    }

}
