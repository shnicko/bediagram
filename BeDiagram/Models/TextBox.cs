using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models;

/// <summary>
/// Free text: multi-line, with optional background, border and font settings. Text boxes can be
/// connected like shapes. <see cref="IsComment"/> flags annotations (sticky-note look, dashed pin lines)
/// so code reading the file can tell notes apart from text that is part of the diagram.
/// </summary>
public class TextBox : IGroupable
{
    public string Id { get; set; } = Node.NewId("t");
    public string Text { get; set; } = "";
    /// <summary>An annotation rather than diagram content.</summary>
    public bool IsComment { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 200;
    public double H { get; set; } = 60;
    public string? GroupId { get; set; }

    /// <summary>Background colour key from <see cref="TextPalette"/>; null = transparent.</summary>
    public string? Fill { get; set; }
    /// <summary>"none" or a line pattern from <see cref="LineLook"/> (solid, dashed, dotted, dashdot, longdash).</summary>
    public string Border { get; set; } = TextLook.NoBorder;
    /// <summary>"light", "regular" or "heavy".</summary>
    public string BorderWeight { get; set; } = LineLook.Regular;
    /// <summary>Border colour key from <see cref="TextPalette"/>; null = default grey.</summary>
    public string? BorderColor { get; set; }
    public bool Rounded { get; set; } = true;

    /// <summary>"sans", "serif" or "mono".</summary>
    public string Font { get; set; } = TextLook.Sans;
    public double FontSize { get; set; } = 14;
    public bool Bold { get; set; }
    /// <summary>Text colour key from <see cref="TextPalette"/>; null = default dark.</summary>
    public string? TextColor { get; set; }
    /// <summary>"left", "center" or "right".</summary>
    public string Align { get; set; } = TextLook.Left;
    /// <summary>"top", "middle" or "bottom".</summary>
    public string VAlign { get; set; } = TextLook.Top;

    public TextBox Copy() { TextBox c = (TextBox)MemberwiseClone(); c.Id = Node.NewId("t"); return c; }

    /// <summary>The standard comment look: yellow sticky note, thin border, Consolas, folded corner.</summary>
    public void ApplyCommentStyle()
    {
        IsComment = true;
        Fill = "yellow"; Border = LineLook.Solid; BorderWeight = LineLook.Light; BorderColor = "yellow"; Rounded = true;
        Font = TextLook.Mono; FontSize = 12; Bold = false; TextColor = "ink";
        Align = TextLook.Left; VAlign = TextLook.Top;
    }

    public static TextBox NewComment()
    {
        TextBox t = new TextBox { W = 200, H = 110 };
        t.ApplyCommentStyle();
        return t;
    }

    /// <summary>Connector look for lines pinned to a comment: light, dashed, no arrows, straight.</summary>
    public static void ApplyPinLook(Edge e)
    {
        e.Style = EdgeStyle.Straight; e.Weight = LineLook.Light; e.Pattern = LineLook.Dashed;
        e.Color = "grey"; e.Arrows = LineLook.ArrowNone;
    }
}
