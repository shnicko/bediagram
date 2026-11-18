using System.Text.Json.Serialization;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Presets;

/// <summary>
/// A saved text box style: background, border, font and alignment, whether it is a comment, the size a new box starts
/// at, and the text when the user chose to save it (null = not saved).
/// </summary>
public class TextBoxPreset
{
    public bool IsComment { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Fill { get; set; }
    public string Border { get; set; } = TextLook.NoBorder;
    public string BorderWeight { get; set; } = LineLook.Regular;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BorderColor { get; set; }
    public bool Rounded { get; set; } = true;
    public string Font { get; set; } = TextLook.Sans;
    public double FontSize { get; set; } = 14;
    public bool Bold { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TextColor { get; set; }
    public string Align { get; set; } = TextLook.Left;
    public string VAlign { get; set; } = TextLook.Top;
    public double W { get; set; }
    public double H { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Text { get; set; }

    /// <summary>The style of <paramref name="t"/>, with its text when <paramref name="text"/> keeps it.</summary>
    public static TextBoxPreset From(TextBox t, PresetText text)
    {
        TextBoxPreset p = new TextBoxPreset();
        p.IsComment = t.IsComment;
        p.Fill = t.Fill;
        p.Border = t.Border;
        p.BorderWeight = t.BorderWeight;
        p.BorderColor = t.BorderColor;
        p.Rounded = t.Rounded;
        p.Font = t.Font;
        p.FontSize = t.FontSize;
        p.Bold = t.Bold;
        p.TextColor = t.TextColor;
        p.Align = t.Align;
        p.VAlign = t.VAlign;
        p.W = t.W;
        p.H = t.H;
        if (PresetTexts.KeepsLabel(text))
        {
            p.Text = t.Text;
        }
        return p;
    }

    /// <summary>Copies the look (and the saved text) onto <paramref name="t"/>. Size, position and whether it is a
    /// comment are left as they are.</summary>
    public void CopyTo(TextBox t)
    {
        t.Fill = Fill;
        t.Border = Border;
        t.BorderWeight = BorderWeight;
        t.BorderColor = BorderColor;
        t.Rounded = Rounded;
        t.Font = Font;
        t.FontSize = FontSize;
        t.Bold = Bold;
        t.TextColor = TextColor;
        t.Align = Align;
        t.VAlign = VAlign;
        if (Text is not null)
        {
            t.Text = Text;
        }
    }
}
