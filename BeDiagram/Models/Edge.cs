using System.Text.Json.Serialization;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Serialization;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models;

public class Edge
{
    public string Id { get; set; } = Node.NewId("e");
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Label { get; set; } = "";
    /// <summary>"elbow" (right-angled, default) or "straight".</summary>
    public EdgeStyle Style { get; set; } = EdgeStyle.Elbow;
    /// <summary>"horizontal" (always level), "along" (follows the line) or "perpendicular" (90° to the line).</summary>
    public LabelOrient LabelOrient { get; set; } = LabelOrient.Along;
    /// <summary>"inline" (on the line; two lines straddle it), "above" or "below" the line, as the text reads.</summary>
    public LabelPlace LabelPlace { get; set; } = LabelPlace.Inline;
    /// <summary>Label position along the connector, 0 = start, 1 = end.</summary>
    public double LabelPos { get; set; } = 0.5;
    /// <summary>"light", "regular" or "heavy".</summary>
    public string Weight { get; set; } = LineLook.Regular;
    /// <summary>"solid", "dashed", "dotted", "dashdot" or "longdash".</summary>
    public string Pattern { get; set; } = LineLook.Solid;
    /// <summary>Palette colour key, or null for the default grey.</summary>
    public string? Color { get; set; }
    /// <summary>"end", "both" or "none": the plain filled arrowheads. Ignored at an end that has a
    /// <see cref="FromHead"/> / <see cref="ToHead"/>. Read the ends with <see cref="StartHead"/> / <see cref="EndHead"/>.</summary>
    public string Arrows { get; set; } = LineLook.ArrowEnd;
    /// <summary>What is drawn at the start (UML or crow's foot, see <see cref="Heads.ArrowHeads"/>). Null (left out of
    /// the file) when <see cref="Arrows"/> says it: connectors with plain arrows are saved as they always were.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(OptionalEnumNameConverter<ArrowHead>))]
    public ArrowHead? FromHead { get; set; }
    /// <summary>What is drawn at the end; see <see cref="FromHead"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(OptionalEnumNameConverter<ArrowHead>))]
    public ArrowHead? ToHead { get; set; }

    /// <summary>Side of the source shape the connector leaves from; null = automatic.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(OptionalEnumNameConverter<Side>))]
    public Side? FromSide { get; set; }
    /// <summary>Side of the target shape the connector arrives at; null = automatic.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(OptionalEnumNameConverter<Side>))]
    public Side? ToSide { get; set; }
    /// <summary>Elbow connectors only: how far (px) the middle segment has been dragged from its automatic position.
    /// Horizontal segments move in y, vertical ones in x. 0 = automatic.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double Bend { get; set; }

    /// <summary>Row of a list / IPO source node the connector leaves from (<see cref="ListItem.Id"/>); null = the whole node.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FromItem { get; set; }
    /// <summary>Row of a list / IPO target node the connector arrives at; null = the whole node.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToItem { get; set; }

    /// <summary>True when either end is on a row; such connectors are drawn above the shapes (canvas and export).</summary>
    [JsonIgnore]
    public bool EndsOnRow
    {
        get
        {
            return FromItem is not null || ToItem is not null;
        }
    }

    /// <summary>Both ends as boxes, looked up with <paramref name="find"/>: the row where an end names one
    /// (<see cref="Kinds.INodeKind.EndAt"/>), otherwise the item itself. Null when either item is missing.</summary>
    public (IBox A, IBox B)? Ends(Func<string, IBox?> find)
    {
        IBox? a = find(From);
        if (a is null)
        {
            return null;
        }
        IBox? b = find(To);
        if (b is null)
        {
            return null;
        }
        return (EndOf(a, FromItem), EndOf(b, ToItem));
    }

    static IBox EndOf(IBox box, string? itemId)
    {
        if (box is Node n)
        {
            return n.Kind.EndAt(n, itemId);
        }
        return box;
    }

    public bool IsManual
    {
        get
        {
            return FromSide is not null || ToSide is not null || Bend != 0;
        }
    }

    /// <summary>Back to fully automatic sides and bend.</summary>
    public void ResetRoute()
    {
        FromSide = null;
        ToSide = null;
        Bend = 0;
    }

    public void CopyRouteFrom(Edge o)
    {
        FromSide = o.FromSide;
        ToSide = o.ToSide;
        Bend = o.Bend;
    }

    /// <summary>The head drawn at the start: <see cref="FromHead"/>, or an arrow when <see cref="Arrows"/> is "both".</summary>
    public ArrowHead StartHead()
    {
        if (FromHead is not null)
        {
            return FromHead.Value;
        }
        if (Arrows == LineLook.ArrowBoth)
        {
            return ArrowHead.Arrow;
        }
        return ArrowHead.None;
    }

    /// <summary>The head drawn at the end: <see cref="ToHead"/>, or an arrow unless <see cref="Arrows"/> is "none".</summary>
    public ArrowHead EndHead()
    {
        if (ToHead is not null)
        {
            return ToHead.Value;
        }
        if (Arrows == LineLook.ArrowNone)
        {
            return ArrowHead.None;
        }
        return ArrowHead.Arrow;
    }

    /// <summary>Sets both ends. Plain arrows that <see cref="Arrows"/> can say (end, both, none) are saved there with
    /// no heads, as connectors always were; anything else is saved in <see cref="FromHead"/> / <see cref="ToHead"/>.</summary>
    public void SetHeads(ArrowHead from, ArrowHead to)
    {
        bool plainFrom = from == ArrowHead.None || from == ArrowHead.Arrow;
        bool plainTo = to == ArrowHead.None || to == ArrowHead.Arrow;
        // "both" needs an arrow at the end too, so an arrow at the start only can't be said with Arrows.
        bool startOnly = from == ArrowHead.Arrow && to == ArrowHead.None;
        if (plainFrom && plainTo && !startOnly)
        {
            FromHead = null;
            ToHead = null;
            if (to == ArrowHead.None)
            {
                Arrows = LineLook.ArrowNone;
            }
            else if (from == ArrowHead.Arrow)
            {
                Arrows = LineLook.ArrowBoth;
            }
            else
            {
                Arrows = LineLook.ArrowEnd;
            }
            return;
        }
        FromHead = from;
        ToHead = to;
        Arrows = LineLook.ArrowNone;
    }

    public void CopyLookFrom(Edge o)
    {
        Style = o.Style;
        Weight = o.Weight;
        Pattern = o.Pattern;
        Color = o.Color;
        Arrows = o.Arrows;
        FromHead = o.FromHead;
        ToHead = o.ToHead;
        LabelOrient = o.LabelOrient;
        LabelPlace = o.LabelPlace;
    }
}
