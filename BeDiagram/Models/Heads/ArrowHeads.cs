using System.Text;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Heads;

/// <summary>Every connector end (<see cref="IArrowHead"/>), in picker order, and the marker definitions a diagram needs.</summary>
public static class ArrowHeads
{
    public static readonly IReadOnlyList<IArrowHead> All = new List<IArrowHead>
    {
        new NoHead(),
        new FilledArrowHead(),
        new OpenArrowHead(),
        new TriangleHead(),
        new DiamondHead(),
        new FilledDiamondHead(),
        new CrowOneHead(),
        new CrowOnlyOneHead(),
        new CrowZeroOrOneHead(),
        new CrowManyHead(),
        new CrowOneOrManyHead(),
        new CrowZeroOrManyHead(),
    };

    static readonly Dictionary<ArrowHead, IArrowHead> ByKey = BuildByKey();

    static Dictionary<ArrowHead, IArrowHead> BuildByKey()
    {
        Dictionary<ArrowHead, IArrowHead> result = new Dictionary<ArrowHead, IArrowHead>();
        foreach (IArrowHead h in All)
        {
            result.Add(h.Key, h);
        }
        return result;
    }

    /// <summary>The heads grouped by <see cref="IArrowHead.Family"/>, in the order they first appear.</summary>
    public static readonly IReadOnlyList<HeadFamily> Families = BuildFamilies();

    static List<HeadFamily> BuildFamilies()
    {
        List<HeadFamily> result = new List<HeadFamily>();
        foreach (IArrowHead h in All)
        {
            HeadFamily? family = null;
            foreach (HeadFamily f in result)
            {
                if (f.Name == h.Family)
                {
                    family = f;
                }
            }
            if (family is null)
            {
                family = new HeadFamily(h.Family, new List<IArrowHead>());
                result.Add(family);
            }
            family.Heads.Add(h);
        }
        return result;
    }

    public static IArrowHead Of(ArrowHead key)
    {
        return ByKey[key];
    }

    /// <summary>The marker reference for one end of a connector ("url(#…)"), or null when that end has no head.</summary>
    public static string? MarkerUrl(Edge e, bool atStart)
    {
        ArrowHead head = atStart ? e.StartHead() : e.EndHead();
        string? id = Of(head).MarkerId(LineLook.Colour(e.Color), e.Weight);
        if (id is null)
        {
            return null;
        }
        return $"url(#{id})";
    }

    /// <summary>The markers for every head the connectors use, other than the plain arrow (whose markers come from
    /// <see cref="LineLook.MarkerDef"/> as before), one per head, colour and weight.</summary>
    public static string MarkerDefs(IEnumerable<Edge> edges)
    {
        List<string> seen = new List<string>();
        StringBuilder sb = new StringBuilder();
        foreach (Edge e in edges)
        {
            string colour = LineLook.Colour(e.Color);
            foreach (ArrowHead head in new ArrowHead[] { e.StartHead(), e.EndHead() })
            {
                if (head == ArrowHead.None || head == ArrowHead.Arrow)
                {
                    continue;
                }
                IArrowHead h = Of(head);
                string? id = h.MarkerId(colour, e.Weight);
                if (id is null || seen.Contains(id))
                {
                    continue;
                }
                seen.Add(id);
                sb.Append(h.MarkerDef(colour, e.Weight));
            }
        }
        return sb.ToString();
    }
}
