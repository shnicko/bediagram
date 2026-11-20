using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Heads;
using BeDiagram.Models.Styles;

namespace BeDiagram.Editing;

/// <summary>Connector ends: plain arrows, UML heads and crow's-foot cardinality (see <see cref="ArrowHeads"/>), set one
/// end at a time, both at once from a relationship, or swapped. Each is one undo step.</summary>
public partial class DiagramEditor
{
    /// <summary>Sets the head at the start (<paramref name="atStart"/>) or the end of a connector.</summary>
    public void SetEdgeHead(Edge e, bool atStart, ArrowHead head)
    {
        ArrowHead from = e.StartHead();
        ArrowHead to = e.EndHead();
        if (atStart)
        {
            from = head;
        }
        else
        {
            to = head;
        }
        if (from == e.StartHead() && to == e.EndHead())
        {
            return;
        }
        Change();
        e.SetHeads(from, to);
    }

    /// <summary>The plain arrow buttons: arrow at the end, at both ends, or none (<see cref="LineLook.ArrowModes"/>).</summary>
    public void SetEdgeArrows(Edge e, string arrows)
    {
        Change();
        e.FromHead = null;
        e.ToHead = null;
        e.Arrows = arrows;
    }

    /// <summary>Both heads and the line pattern of a UML or entity relationship.</summary>
    public void ApplyRelationship(Edge e, Relationship r)
    {
        Change();
        e.SetHeads(r.From, r.To);
        e.Pattern = r.Pattern;
    }

    /// <summary>True when the connector's heads and pattern are exactly those of <paramref name="r"/>.</summary>
    public static bool IsRelationship(Edge e, Relationship r)
    {
        return e.StartHead() == r.From && e.EndHead() == r.To && e.Pattern == r.Pattern;
    }

    /// <summary>Swaps the start and end heads (the line keeps its direction and route).</summary>
    public void SwapHeads(Edge e)
    {
        ArrowHead from = e.StartHead();
        ArrowHead to = e.EndHead();
        if (from == to)
        {
            return;
        }
        Change();
        e.SetHeads(to, from);
    }
}
