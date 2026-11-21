using System.Text.Json.Serialization;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Serialization;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Presets;

/// <summary>
/// A saved connector style: elbow or straight, weight, line type, colour, arrowheads (or UML / crow's-foot ends) and label placement, and the
/// label when the user chose to save it (null = not saved). Connectors need two ends, so this style is applied from
/// the connector panel and isn't placed from the sidebar.
/// </summary>
public class ConnectorPreset
{
    public EdgeStyle Style { get; set; } = EdgeStyle.Elbow;
    public string Weight { get; set; } = LineLook.Regular;
    public string Pattern { get; set; } = LineLook.Solid;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Color { get; set; }
    public string Arrows { get; set; } = LineLook.ArrowEnd;
    /// <summary>UML or crow's-foot ends (see <see cref="Edge.FromHead"/>); null when <see cref="Arrows"/> says it all.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(OptionalEnumNameConverter<ArrowHead>))]
    public ArrowHead? FromHead { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(OptionalEnumNameConverter<ArrowHead>))]
    public ArrowHead? ToHead { get; set; }
    public LabelOrient LabelOrient { get; set; } = LabelOrient.Along;
    public LabelPlace LabelPlace { get; set; } = LabelPlace.Inline;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; set; }

    /// <summary>The style of <paramref name="e"/>, with its label when <paramref name="text"/> keeps it.</summary>
    public static ConnectorPreset From(Edge e, PresetText text)
    {
        ConnectorPreset p = new ConnectorPreset();
        p.Style = e.Style;
        p.Weight = e.Weight;
        p.Pattern = e.Pattern;
        p.Color = e.Color;
        p.Arrows = e.Arrows;
        p.FromHead = e.FromHead;
        p.ToHead = e.ToHead;
        p.LabelOrient = e.LabelOrient;
        p.LabelPlace = e.LabelPlace;
        if (PresetTexts.KeepsLabel(text))
        {
            p.Label = e.Label;
        }
        return p;
    }

    /// <summary>Copies the look (and the saved label) onto <paramref name="e"/>. Its ends and route are left alone.</summary>
    public void CopyTo(Edge e)
    {
        e.Style = Style;
        e.Weight = Weight;
        e.Pattern = Pattern;
        e.Color = Color;
        e.Arrows = Arrows;
        e.FromHead = FromHead;
        e.ToHead = ToHead;
        e.LabelOrient = LabelOrient;
        e.LabelPlace = LabelPlace;
        if (Label is not null)
        {
            e.Label = Label;
        }
    }
}
