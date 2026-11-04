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
                 + $"<g transform=\"translate({F(lay.IconX)},{F(lay.IconY)}) scale({F(lay.IconSize / 24)})\" fill=\"none\" stroke=\"{st.IconColor}\" stroke-width=\"3\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Icons.Get(Geometry.IconOf(n))}</g></svg>");
    }
}
