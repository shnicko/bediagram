using System.Text;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Shapes;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Text;

namespace BeDiagram.Models;

/// <summary>Geometry of boxes (shapes, text boxes, groups): hit tests, bounds around a set of boxes, the drawing's extent, and how far a major step's halo reaches.</summary>
public static class Boxes
{
    /// <summary>Gap between a major step's outline and its halo ring, and the ring's width.</summary>
    public const double HaloGap = 6, HaloRing = 2;


    public static bool Contains(IBox b, double x, double y)
    {
        return x >= b.X && x <= b.X + b.W && y >= b.Y && y <= b.Y + b.H;
    }

    public static bool Intersects(IBox b, double x1, double y1, double x2, double y2)
    {
        return b.X < Math.Max(x1, x2) && b.X + b.W > Math.Min(x1, x2) &&
               b.Y < Math.Max(y1, y2) && b.Y + b.H > Math.Min(y1, y2);
    }

    /// <summary>Bounding box of everything on the canvas (null when empty).</summary>
    public static (double X, double Y, double W, double H)? ContentBounds(Diagram d)
    {
        List<IBox> boxes = new List<IBox>();
        boxes.AddRange(d.Nodes);
        boxes.AddRange(d.TextBoxes);
        boxes.AddRange(d.Groups);
        if (boxes.Count == 0)
        {
            return null;
        }
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;
        foreach (IBox b in boxes)
        {
            minX = Math.Min(minX, b.X);
            minY = Math.Min(minY, b.Y);
            maxX = Math.Max(maxX, b.X + b.W);
            maxY = Math.Max(maxY, b.Y + b.H + TextLayout.ExtraBelow(b));
        }
        // Labels under circles/outcomes and descriptions can be wider than the shape.
        foreach (Node n in d.Nodes)
        {
            (double X, double Y, double W, double H)? r = TextLayout.OutsideTextBox(n, TextLayout.Layout(n));
            if (r is null)
            {
                continue;
            }
            minX = Math.Min(minX, r.Value.X);
            maxX = Math.Max(maxX, r.Value.X + r.Value.W);
        }
        return (minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>Bounds that wrap a set of boxes plus padding, with room for a title bar at the top (a new group).</summary>
    public static (double X, double Y, double W, double H) GroupBounds(IEnumerable<IBox> boxes)
    {
        return GroupBounds(boxes, Group.Header, 0);
    }

    /// <summary>Bounds that wrap a set of boxes plus padding, with room for this group's title where it sits inside.</summary>
    public static (double X, double Y, double W, double H) GroupBounds(IEnumerable<IBox> boxes, Group g)
    {
        return GroupBounds(boxes, g.InsetTop(), g.InsetBottom());
    }

    static (double X, double Y, double W, double H) GroupBounds(IEnumerable<IBox> boxes, double insetTop, double insetBottom)
    {
        List<IBox> list = new List<IBox>(boxes);
        if (list.Count == 0)
        {
            throw new InvalidOperationException("Sequence contains no elements");
        }
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;
        foreach (IBox b in list)
        {
            minX = Math.Min(minX, b.X);
            minY = Math.Min(minY, b.Y);
            maxX = Math.Max(maxX, b.X + b.W);
            maxY = Math.Max(maxY, b.Y + b.H + TextLayout.ExtraBelow(b));
        }
        return (minX - Group.Pad, minY - Group.Pad - insetTop, maxX - minX + Group.Pad * 2, maxY - minY + Group.Pad * 2 + insetTop + insetBottom);
    }

    /// <summary>How far a box's outer edge sits beyond its outline: the halo ring's outside edge for a major step,
    /// 0 for everything else. Connectors stop here (so arrowheads land on the ring, not under it) and text under the
    /// shape starts this much lower.</summary>
    public static double HaloOut(IBox b)
    {
        if (b is Node n && Emphases.Of(n).HasHalo)
        {
            return HaloGap + HaloRing;
        }
        return 0;
    }
}
