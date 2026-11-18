using System.Text.Json.Serialization;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Models.Presets;

/// <summary>
/// A saved shape style: what a new shape placed from it starts as (type, circle, outcome, size) and the look that
/// "Apply" copies onto existing shapes (colour, solid fill, emphasis, text background, and the icon when one was
/// saved). Label and description are kept only when the user chose to save them; null = not saved.
/// </summary>
public class ShapePreset
{
    public NodeType Type { get; set; } = NodeType.Process;
    public bool Round { get; set; }
    [JsonConverter(typeof(OptionalEnumNameConverter<OutcomeType>))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OutcomeType? Outcome { get; set; }
    public double W { get; set; }
    public double H { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Color { get; set; }
    public bool Filled { get; set; }
    public EmphasisLevel Emphasis { get; set; }
    public bool ClearTextBack { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Icon { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Label { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; set; }

    /// <summary>The style of <paramref name="n"/>, with the text <paramref name="text"/> asks for.</summary>
    public static ShapePreset From(Node n, PresetText text)
    {
        ShapePreset p = new ShapePreset();
        p.Type = n.Type;
        p.Round = n.Round;
        p.Outcome = n.Outcome;
        p.W = n.W;
        p.H = n.H;
        p.Color = n.Color;
        p.Filled = n.Filled;
        p.Emphasis = n.Emphasis;
        p.ClearTextBack = n.ClearTextBack;
        p.Icon = n.Icon;
        if (PresetTexts.KeepsLabel(text))
        {
            p.Label = n.Label;
        }
        if (PresetTexts.KeepsDescription(text) && !string.IsNullOrEmpty(n.Description))
        {
            p.Description = n.Description;
        }
        return p;
    }

    /// <summary>What the sidebar adds for this style: its type, outcome and circle look.</summary>
    [JsonIgnore]
    public PaletteItem Item
    {
        get
        {
            return new PaletteItem(Type, Outcome, Round);
        }
    }

    /// <summary>Copies the look onto <paramref name="n"/>: colour, fill, emphasis, text background, the icon when one
    /// was saved, and the saved text. Type, outcome, circle and size are left as they are.</summary>
    public void CopyTo(Node n)
    {
        n.Color = Color;
        n.Filled = Filled;
        n.Emphasis = Emphasis;
        n.ClearTextBack = ClearTextBack;
        if (Icon is not null)
        {
            n.Icon = Icon;
        }
        if (Label is not null)
        {
            n.Label = Label;
        }
        if (Description is not null)
        {
            n.Description = Description;
        }
    }
}
