using System.Xml.Linq;
using static BeDiagram.Services.Export.Pdf.SvgValues;

namespace BeDiagram.Services.Export.Pdf;

/// <summary>The inherited SVG presentation attributes at an element (fill, stroke, dash, font, …).</summary>
internal record PdfStyle(string Fill, string Stroke, double StrokeWidth, double FillOpacity, double StrokeOpacity,
             string? Dash, string Cap, string Join, double FontSize, string Weight, string Family, string Anchor,
             bool StrokeUnder = false)
{
    public static readonly PdfStyle Default = new("#000", "none", 1, 1, 1, null, "butt", "miter", 16, "normal", "sans-serif", "start");

    public PdfStyle With(XElement e)
    {
        PdfStyle s = this;
        double opacity = Num(Attr(e, "opacity"), 1);
        s = s with
        {
            Fill = Attr(e, "fill") ?? s.Fill,
            Stroke = Attr(e, "stroke") ?? s.Stroke,
            StrokeWidth = Num(Attr(e, "stroke-width"), s.StrokeWidth),
            FillOpacity = Num(Attr(e, "fill-opacity"), s.FillOpacity) * opacity,
            StrokeOpacity = Num(Attr(e, "stroke-opacity"), s.StrokeOpacity) * opacity,
            Dash = Attr(e, "stroke-dasharray") is { } d ? (d == "none" ? null : d) : s.Dash,
            Cap = Attr(e, "stroke-linecap") ?? s.Cap,
            Join = Attr(e, "stroke-linejoin") ?? s.Join,
            FontSize = Num(Attr(e, "font-size")?.Replace("px", ""), s.FontSize),
            Weight = Attr(e, "font-weight") ?? s.Weight,
            Family = Attr(e, "font-family") ?? s.Family,
            Anchor = Attr(e, "text-anchor") ?? s.Anchor,
            // paint-order="stroke": stroke drawn under the fill (used for white halos behind text).
            StrokeUnder = Attr(e, "paint-order") is { } po ? po.TrimStart().StartsWith("stroke") : s.StrokeUnder,
        };
        return s;
    }
}
