using System.Globalization;
using BeDiagram.Models;
using Microsoft.AspNetCore.Components;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;

namespace BeDiagram.Components;

/// <summary>Small inline SVGs used by the editor UI (toolbar, palette, panel). Imported with <c>@using static</c>.</summary>
public static class UiSvg
{
    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>A Lucide icon at the given pixel size, drawn in the current text colour.</summary>
    public static MarkupString IconSvg(string name, int size) =>
        new($"<svg width=\"{size}\" height=\"{size}\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Icons.Get(name)}</svg>");

    /// <summary>Small solid outcome symbol for the palette and the panel (same geometry as the canvas).</summary>
    public static MarkupString OutcomeSwatch(string outcome, int size)
    {
        Node n = new Node { Type = NodeTypes.Outcome, Outcome = outcome, Filled = true, W = 24, H = 24 };
        NodeStyle st = NodeStyle.For(n);
        LabelLayout lay = Geometry.Layout(n, "");
        return new($"<svg width=\"{size}\" height=\"{size}\" viewBox=\"-1 -1 26 26\"><path d=\"{Geometry.ShapePath(n)}\" fill=\"{st.Fill}\"/>"
                 + $"<g transform=\"translate({F(lay.IconX)},{F(lay.IconY)}) scale({F(lay.IconSize / 24)})\" fill=\"none\" stroke=\"{st.IconColor}\" stroke-width=\"3\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Icons.Get(n.Kind.IconOf(n))}</g></svg>");
    }

    /// <summary>A shape drawn as a circle with its type's icon, for the palette (same colours as the canvas).</summary>
    public static MarkupString RoundSwatch(string type, int size)
    {
        NodeTypeInfo t = NodeTypes.Get(type);
        string icon = t.RoundIcon;
        return new($"<svg width=\"{size}\" height=\"{size}\" viewBox=\"-2 -2 28 28\"><circle cx=\"12\" cy=\"12\" r=\"12\" fill=\"{t.Fill}\" stroke=\"{t.Stroke}\" stroke-width=\"1.6\"/>"
                 + $"<g transform=\"translate(6,6) scale(0.5)\" fill=\"none\" stroke=\"{t.Stroke}\" stroke-width=\"2.4\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Icons.Get(icon)}</g></svg>");
    }

    /// <summary>A short sample of a line (weight, pattern, arrowheads) for the style buttons in the panels.</summary>
    public static MarkupString LinePreview(string weight, string pattern, string arrows)
    {
        double w = LineLook.Width(weight);
        string? dash = LineLook.Dash(pattern, w);
        int x1 = arrows == LineLook.ArrowBoth ? 7 : 2;
        int x2 = arrows == LineLook.ArrowNone ? 38 : 33;
        string head = "";
        if (arrows != LineLook.ArrowNone) head += "<path d=\"M32,2 L40,6 L32,10 z\" fill=\"currentColor\"/>";
        if (arrows == LineLook.ArrowBoth) head += "<path d=\"M8,2 L0,6 L8,10 z\" fill=\"currentColor\"/>";
        return new($"<svg width=\"40\" height=\"12\" viewBox=\"0 0 40 12\"><line x1=\"{x1}\" y1=\"6\" x2=\"{x2}\" y2=\"6\" stroke=\"currentColor\" stroke-width=\"{F(w)}\"{(dash is null ? "" : $" stroke-dasharray=\"{dash}\"")} stroke-linecap=\"{LineLook.Cap(pattern)}\"/>{head}</svg>");
    }
}
