using Microsoft.AspNetCore.Components;
using BeDiagram.Models;
using BeDiagram.Models.Presets;
using BeDiagram.Models.Styles;
using BeDiagram.Rendering;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Components;

/// <summary>Small pictures of saved styles for the sidebar's Saved styles list, in the style's own colours.</summary>
public static class PresetPreview
{
    const int Width = 32;
    const int Height = 22;

    /// <summary>The shape's outline (or circle) with its fill, outline and icon.</summary>
    public static MarkupString Shape(ShapePreset p)
    {
        Node n = new Node();
        n.Type = p.Type;
        n.Round = p.Round;
        n.Outcome = p.Outcome;
        n.Color = p.Color;
        n.Filled = p.Filled;
        n.Emphasis = p.Emphasis;
        n.Icon = p.Icon;
        // Circles and outcomes are square; other shapes keep a landscape box.
        if (p.Round || p.Outcome is not null)
        {
            n.X = 6;
            n.W = 20;
            n.H = 20;
        }
        else
        {
            n.W = 30;
            n.H = 20;
        }
        NodeStyle st = NodeStyle.For(n);
        string icon = "";
        string? iconName = n.Kind.IconOf(n);
        if (iconName is not null)
        {
            double cx = n.X + n.W / 2 - 6;
            double cy = n.Y + n.H / 2 - 6;
            icon = $"<g transform=\"translate({F(cx)},{F(cy)}) scale(0.5)\" fill=\"none\" stroke=\"{st.IconColor}\" stroke-width=\"2.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Icons.Get(iconName)}</g>";
        }
        return Svg($"<path d=\"{SvgShapes.ShapePath(n)}\" fill=\"{st.Fill}\" stroke=\"{st.Stroke}\" stroke-width=\"1.5\"/>{icon}");
    }

    /// <summary>A box with the group's tint, border and title band.</summary>
    public static MarkupString Group(GroupPreset p)
    {
        Swatch sw = Palette.Get(p.Color);
        string fill = "none";
        if (p.Filled)
        {
            fill = sw.Light;
        }
        string dash = "";
        string? pattern = LineLook.Dash(p.Border, 1.2);
        if (pattern is not null)
        {
            dash = $" stroke-dasharray=\"{pattern}\"";
        }
        string band = "";
        if (p.HeaderStyle == GroupLook.Band)
        {
            double y = 1;
            if (p.HeaderPos == GroupLook.Bottom)
            {
                y = 14;
            }
            band = $"<rect x=\"1\" y=\"{F(y)}\" width=\"30\" height=\"6\" fill=\"{sw.Stroke}\" fill-opacity=\"0.25\"/>";
        }
        return Svg($"<rect x=\"1\" y=\"1\" width=\"30\" height=\"19\" rx=\"3\" fill=\"{fill}\" stroke=\"{sw.Stroke}\" stroke-width=\"1.2\"{dash}/>{band}");
    }

    /// <summary>A box with the text box's background and border, and two lines in its text colour.</summary>
    public static MarkupString TextBox(TextBoxPreset p)
    {
        string fill = Palette.FillOf(p.Fill) ?? "#ffffff";
        string stroke = "#cfd4da";
        string dash = " stroke-dasharray=\"2 2\"";
        if (p.Border != TextLook.NoBorder)
        {
            stroke = Palette.StrokeOf(p.BorderColor, TextStyle.DefaultBorder);
            dash = "";
        }
        string ink = Palette.StrokeOf(p.TextColor, TextStyle.DefaultText);
        double weight = 1.5;
        if (p.Bold)
        {
            weight = 2.5;
        }
        return Svg($"<rect x=\"1\" y=\"1\" width=\"30\" height=\"19\" rx=\"2\" fill=\"{fill}\" stroke=\"{stroke}\" stroke-width=\"1\"{dash}/>"
                 + $"<path d=\"M6,8 H26 M6,13 H20\" stroke=\"{ink}\" stroke-width=\"{F(weight)}\" stroke-linecap=\"round\"/>");
    }

    static MarkupString Svg(string body)
    {
        return new MarkupString($"<svg width=\"{Width}\" height=\"{Height}\" viewBox=\"-1 -1 {Width + 2} {Height + 2}\">{body}</svg>");
    }
}
