using System.Text.Json.Serialization;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Presets;

/// <summary>
/// A saved group style: colour, tint, border, title position and style, the size a new group starts at, and the
/// title when the user chose to save it (null = not saved).
/// </summary>
public class GroupPreset
{
    public string Color { get; set; } = Palette.DefaultColour;
    public bool Filled { get; set; } = true;
    public string Border { get; set; } = LineLook.Solid;
    public string BorderWeight { get; set; } = LineLook.Regular;
    public string HeaderPos { get; set; } = GroupLook.Top;
    public string HeaderStyle { get; set; } = GroupLook.Band;
    public double W { get; set; }
    public double H { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Title { get; set; }

    /// <summary>The style of <paramref name="g"/>, with its title when <paramref name="text"/> keeps it.</summary>
    public static GroupPreset From(Group g, PresetText text)
    {
        GroupPreset p = new GroupPreset();
        p.Color = g.Color;
        p.Filled = g.Filled;
        p.Border = g.Border;
        p.BorderWeight = g.BorderWeight;
        p.HeaderPos = g.HeaderPos;
        p.HeaderStyle = g.HeaderStyle;
        p.W = g.W;
        p.H = g.H;
        if (PresetTexts.KeepsLabel(text))
        {
            p.Title = g.Title;
        }
        return p;
    }

    /// <summary>Copies the look (and the saved title) onto <paramref name="g"/>. Size and contents are left alone.</summary>
    public void CopyTo(Group g)
    {
        g.Color = Color;
        g.Filled = Filled;
        g.Border = Border;
        g.BorderWeight = BorderWeight;
        g.HeaderPos = HeaderPos;
        g.HeaderStyle = HeaderStyle;
        if (Title is not null)
        {
            g.Title = Title;
        }
    }
}
