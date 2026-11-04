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

    public bool IsManual => FromSide is not null || ToSide is not null || Bend != 0;

    /// <summary>Back to fully automatic sides and bend.</summary>
    public void ResetRoute() { FromSide = null; ToSide = null; Bend = 0; }

    public void CopyRouteFrom(Edge o) { FromSide = o.FromSide; ToSide = o.ToSide; Bend = o.Bend; }

    public void CopyLookFrom(Edge o) { Style = o.Style; Weight = o.Weight; Pattern = o.Pattern; Color = o.Color; Arrows = o.Arrows; LabelOrient = o.LabelOrient; LabelPlace = o.LabelPlace; }
}
