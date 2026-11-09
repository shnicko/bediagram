using System.Globalization;
using System.Net;
using System.Text;
using BeDiagram.Models;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;

namespace BeDiagram.Services;

/// <summary>
/// SVG markup for the inside of a list / IPO node: the title band, IPO section titles and dividers, and the rows
/// (white boxes with an icon, a title and a description). The canvas and the SVG export both use it, drawn over the
/// node's outline and under its title, so they always match. Layout comes from <see cref="PanelGeometry"/>.
/// </summary>
public static class PanelSvg
{
    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    static string E(string s) => WebUtility.HtmlEncode(s ?? "");

    public const string RowText = "#1f2933";
    public const string RowDesc = "#55606e";

    /// <summary>Everything inside the panel except its title text, or an empty string for other shapes.</summary>
    public static string Body(Node n, NodeStyle st)
    {
        if (!n.Kind.IsPanel) return "";
        StringBuilder sb = new StringBuilder();
        // On a solid fill the band and dividers are white, otherwise a tint of the outline colour.
        string tint = n.Filled ? "#ffffff" : st.Stroke;
        sb.Append($"<path d=\"{PanelGeometry.HeaderBandPath(n)}\" fill=\"{tint}\" fill-opacity=\"{(n.Filled ? "0.18" : "0.14")}\"/>");
        foreach (PanelSection s in PanelGeometry.Sections(n))
        {
            if (s.Divider is { } d)
                sb.Append($"<path d=\"{d}\" fill=\"none\" stroke=\"{tint}\" stroke-opacity=\"0.4\" stroke-width=\"1\"/>");
            sb.Append($"<text x=\"{F(s.TitleX)}\" y=\"{F(s.TitleY)}\" font-size=\"{F(PanelGeometry.SectionFontSize)}\" font-weight=\"600\" text-anchor=\"{s.Anchor}\" fill=\"{(n.Filled ? "#ffffff" : st.Stroke)}\">{E(s.Title)}</text>");
        }
        foreach (PanelRow r in PanelGeometry.Rows(n))
        {
            sb.Append($"<rect x=\"{F(r.Box.X)}\" y=\"{F(r.Box.Y)}\" width=\"{F(r.Box.W)}\" height=\"{F(r.Box.H)}\" rx=\"8\" fill=\"#ffffff\" stroke=\"{st.Stroke}\" stroke-opacity=\"0.6\" stroke-width=\"1.2\"/>");
            if (r.HasIcon)
                sb.Append($"<g transform=\"translate({F(r.IconX)},{F(r.IconY)}) scale({F(PanelGeometry.IconSize / 24)})\" fill=\"none\" stroke=\"{st.Stroke}\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Icons.Get(r.Item.Icon)}</g>");
            sb.Append($"<text x=\"{F(r.TextX)}\" y=\"{F(r.TitleY)}\" font-size=\"{F(PanelGeometry.TitleFontSize)}\" font-weight=\"600\" fill=\"{RowText}\">{E(r.Title)}</text>");
            if (r.Desc is { } desc)
                sb.Append($"<text x=\"{F(r.TextX)}\" y=\"{F(r.DescY)}\" font-size=\"{F(Geometry.DescFontSize)}\" fill=\"{RowDesc}\">{E(desc)}</text>");
        }
        return sb.ToString();
    }
}
