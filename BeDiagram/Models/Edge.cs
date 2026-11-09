using System.Text.Json.Serialization;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models;

public class Edge
{
    public string Id { get; set; } = Node.NewId("e");
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public string Label { get; set; } = "";
    /// <summary>"elbow" (right-angled, default) or "straight".</summary>
    public string Style { get; set; } = EdgeStyles.Elbow;
    /// <summary>"horizontal" (always level), "along" (follows the line) or "perpendicular" (90° to the line).</summary>
    public string LabelOrient { get; set; } = LabelOrients.Along;
    /// <summary>"inline" (on the line; two lines straddle it), "above" or "below" the line, as the text reads.</summary>
    public string LabelPlace { get; set; } = LabelPlaces.Inline;
    /// <summary>Label position along the connector, 0 = start, 1 = end.</summary>
    public double LabelPos { get; set; } = 0.5;
    /// <summary>"light", "regular" or "heavy".</summary>
    public string Weight { get; set; } = LineLook.Regular;
    /// <summary>"solid", "dashed", "dotted", "dashdot" or "longdash".</summary>
    public string Pattern { get; set; } = LineLook.Solid;
    /// <summary>Palette colour key, or null for the default grey.</summary>
    public string? Color { get; set; }
    /// <summary>"end", "both" or "none".</summary>
    public string Arrows { get; set; } = LineLook.ArrowEnd;

    /// <summary>Side of the source shape the connector leaves from ("top", "right", "bottom", "left"); null = automatic.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FromSide { get; set; }
    /// <summary>Side of the target shape the connector arrives at; null = automatic.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ToSide { get; set; }
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
    public bool EndsOnRow => FromItem is not null || ToItem is not null;

    /// <summary>Both ends as boxes, looked up with <paramref name="find"/>: the row where an end names one
    /// (<see cref="Kinds.INodeKind.EndAt"/>), otherwise the item itself. Null when either item is missing.</summary>
    public (IBox A, IBox B)? Ends(Func<string, IBox?> find) =>
        find(From) is { } a && find(To) is { } b ? (EndOf(a, FromItem), EndOf(b, ToItem)) : null;

    static IBox EndOf(IBox box, string? itemId) => box is Node n ? n.Kind.EndAt(n, itemId) : box;

    public bool IsManual => FromSide is not null || ToSide is not null || Bend != 0;

    /// <summary>Back to fully automatic sides and bend.</summary>
    public void ResetRoute() { FromSide = null; ToSide = null; Bend = 0; }

    public void CopyRouteFrom(Edge o) { FromSide = o.FromSide; ToSide = o.ToSide; Bend = o.Bend; }

    public void CopyLookFrom(Edge o) { Style = o.Style; Weight = o.Weight; Pattern = o.Pattern; Color = o.Color; Arrows = o.Arrows; LabelOrient = o.LabelOrient; LabelPlace = o.LabelPlace; }
}
