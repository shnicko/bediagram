using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;

namespace BeDiagram.Models.Routing;

/// <summary>Straight connectors: a line between the shapes' outlines, aimed centre to centre; a pinned end starts at
/// its side's middle.</summary>
public class StraightRouting : IConnectorRouting
{
    public EdgeStyle Style => EdgeStyle.Straight;

    /// <summary>Straight line between the shapes' outlines, aimed centre to centre; pinned ends start at a side middle.</summary>
    public EdgeRoute Route(IBox a, IBox b, Side? fromSide, Side? toSide, double bend)
    {
        (double X, double Y) ca = (a.X + a.W / 2, a.Y + a.H / 2), cb = (b.X + b.W / 2, b.Y + b.H / 2);
        (double X, double Y)? p1 = fromSide is { } fs ? EdgeRouter.SidePoint(EdgeRouter.ConnectBox(a), fs) : null;
        (double X, double Y)? p2 = toSide is { } ts ? EdgeRouter.SidePoint(EdgeRouter.ConnectBox(b), ts) : null;
        (double X, double Y) aimA = p2 ?? cb;
        (double X, double Y) aimB = p1 ?? ca;
        (double X, double Y) start = p1 ?? EdgeRouter.ConnectBoundary(a, aimA.X, aimA.Y);
        (double X, double Y) end = p2 ?? EdgeRouter.ConnectBoundary(b, aimB.X, aimB.Y);
        return new EdgeRoute(new() { start, end }, -1, fromSide, toSide);
    }
}
