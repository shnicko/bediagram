using System.Net;
using System.Text;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Routing;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Text;
using BeDiagram.Rendering;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Services.Export;

/// <summary>
/// Writes a diagram as a standalone SVG: the shared renderer behind the SVG, PNG and PDF export formats (PNG and PDF
/// are drawn from this SVG, so all three match). Uses the same routing, text layout, kinds, outlines and <c>SvgShapes</c> markup as the
/// canvas. Registered once in Program.cs and given to the formats that need it.
/// </summary>
public class SvgDiagramWriter
{
    static string E(string s) => WebUtility.HtmlEncode(s ?? "");

    /// <summary>
    /// Standalone SVG of the diagram. Without a view: the page (when a size is set) or the drawing extents.
    /// With a view: just that window, clipped — at screen size, or fitted to the page when a size is set.
    /// <paramref name="background"/> = false leaves out the white background (transparent PNG export).
    /// </summary>
    public string Write(Diagram d, ExportView? view = null, bool background = true)
    {
        Dictionary<string, IBox> lookup = new Dictionary<string, IBox>();
        foreach (IBox b in AllBoxes(d))
        {
            lookup.Add(b.Id, b);
        }
        StringBuilder sb = new StringBuilder();
        string closeContent = WriteOpening(sb, d, view, background, MarkerDefs(d));

        // Drawing order: a group inside another is drawn over it.
        foreach (Group g in new GroupTree(d).PaintOrder())
        {
            sb.Append(GroupSvg(g));
        }

        foreach (TextBox t in d.TextBoxes)
        {
            sb.Append(TextBoxSvg(t));
        }

        StringBuilder labels = new StringBuilder();
        // Connectors ending on a row of a list / IPO node go above the shapes, so that end isn't hidden.
        StringBuilder rowEdges = new StringBuilder();
        foreach (Edge e in d.Edges)
        {
            StringBuilder lines = sb;
            if (e.EndsOnRow)
            {
                lines = rowEdges;
            }
            WriteEdge(lines, labels, e, lookup);
        }

        foreach (Node n in d.Nodes)
        {
            WriteNode(sb, d, n);
        }
        sb.Append(rowEdges);
        // Group link badges sit above the members so they're never hidden.
        foreach (Group g in d.Groups)
        {
            if (SvgShapes.HasLink(g))
            {
                sb.Append(SvgShapes.LinkBadgeSvg(g, Palette.Get(g.Color).Stroke)).Append('\n');
            }
        }
        sb.Append(labels);
        sb.Append(closeContent);
        sb.Append("</svg>\n");
        return sb.ToString();
    }

    /// <summary>Every shape, text box and group, in that order.</summary>
    static List<IBox> AllBoxes(Diagram d)
    {
        List<IBox> result = new List<IBox>();
        foreach (Node n in d.Nodes)
        {
            result.Add(n);
        }
        foreach (TextBox t in d.TextBoxes)
        {
            result.Add(t);
        }
        foreach (Group g in d.Groups)
        {
            result.Add(g);
        }
        return result;
    }

    /// <summary>The &lt;defs&gt; block: one arrow marker per colour and weight the connectors use.</summary>
    static string MarkerDefs(Diagram d)
    {
        List<(string Colour, string Weight)> markers = new List<(string Colour, string Weight)>();
        foreach (Edge x in d.Edges)
        {
            (string Colour, string Weight) m = (LineLook.Colour(x.Color), x.Weight);
            if (!markers.Contains(m))
            {
                markers.Add(m);
            }
        }
        StringBuilder sb = new StringBuilder("<defs>");
        foreach ((string Colour, string Weight) m in markers)
        {
            sb.Append(LineLook.MarkerDef(m.Colour, m.Weight));
        }
        sb.Append("</defs>\n");
        return sb.ToString();
    }

    /// <summary>
    /// The &lt;svg&gt; element, defs and white background, and the transform that places the drawing (fitted to the
    /// page or scaled down to fit the margins). Returns the markup that closes that transform ("" when there is none).
    /// </summary>
    static string WriteOpening(StringBuilder sb, Diagram d, ExportView? view, bool background, string defs)
    {
        string closeContent = "";
        if (view is not null)
        {
            if (d.Page.Enabled)
            {
                (double wmm, double hmm) = d.Page.Mm;
                double W = d.Page.WidthPx;
                double H = d.Page.HeightPx;
                double m = d.Page.MarginPx;
                double availW = Math.Max(1, W - 2 * m);
                double availH = Math.Max(1, H - 2 * m);
                double scale = Math.Min(availW / view.W, availH / view.H);
                double tx = m + (availW - view.W * scale) / 2 - view.X * scale;
                double ty = m + (availH - view.H * scale) / 2 - view.Y * scale;
                sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(wmm)}mm\" height=\"{F(hmm)}mm\" viewBox=\"0 0 {F(W)} {F(H)}\" font-family=\"Segoe UI, Arial, sans-serif\">\n");
                sb.Append(defs.Replace("</defs>", $"<clipPath id=\"view\"><rect x=\"{F(view.X)}\" y=\"{F(view.Y)}\" width=\"{F(view.W)}\" height=\"{F(view.H)}\"/></clipPath></defs>"));
                if (background)
                {
                    sb.Append($"<rect x=\"0\" y=\"0\" width=\"{F(W)}\" height=\"{F(H)}\" fill=\"#ffffff\"/>\n");
                }
                sb.Append($"<g transform=\"translate({F(tx)},{F(ty)}) scale({F(scale)})\"><g clip-path=\"url(#view)\">\n");
                closeContent = "</g></g>\n";
            }
            else
            {
                // Exactly the screen: the viewBox does the clipping.
                sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{F(view.X)} {F(view.Y)} {F(view.W)} {F(view.H)}\" width=\"{F(view.ScreenW)}\" height=\"{F(view.ScreenH)}\" font-family=\"Segoe UI, Arial, sans-serif\">\n");
                sb.Append(defs);
                if (background)
                {
                    sb.Append($"<rect x=\"{F(view.X)}\" y=\"{F(view.Y)}\" width=\"{F(view.W)}\" height=\"{F(view.H)}\" fill=\"#ffffff\"/>\n");
                }
            }
        }
        else if (d.Page.Enabled)
        {
            // Fixed paper size: physical mm dimensions so it drops into Word/PowerPoint at the right size.
            (double wmm, double hmm) = d.Page.Mm;
            double W = d.Page.WidthPx;
            double H = d.Page.HeightPx;
            double m = d.Page.MarginPx;
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(wmm)}mm\" height=\"{F(hmm)}mm\" viewBox=\"0 0 {F(W)} {F(H)}\" font-family=\"Segoe UI, Arial, sans-serif\">\n");
            sb.Append(defs);
            if (background)
            {
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{F(W)}\" height=\"{F(H)}\" fill=\"#ffffff\"/>\n");
            }
            (double X, double Y, double W, double H)? bounds = Boxes.ContentBounds(d);
            if (bounds is not null && d.Page.FitOnExport)
            {
                (double X, double Y, double W, double H) cb = bounds.Value;
                double availW = W - 2 * m;
                double availH = H - 2 * m;
                bool fits = cb.X >= m - 0.5 && cb.Y >= m - 0.5 && cb.X + cb.W <= W - m + 0.5 && cb.Y + cb.H <= H - m + 0.5;
                // Too big or outside the margins: scale down (never up) and centre inside the margins.
                if (!fits && availW > 0 && availH > 0)
                {
                    double scale = Math.Min(1, Math.Min(availW / cb.W, availH / cb.H));
                    double tx = m + (availW - cb.W * scale) / 2 - cb.X * scale;
                    double ty = m + (availH - cb.H * scale) / 2 - cb.Y * scale;
                    sb.Append($"<g transform=\"translate({F(tx)},{F(ty)}) scale({F(scale)})\">\n");
                    closeContent = "</g>\n";
                }
            }
        }
        else
        {
            double minX = 0;
            double minY = 0;
            double maxX = 400;
            double maxY = 300;
            (double X, double Y, double W, double H)? bounds = Boxes.ContentBounds(d);
            if (bounds is not null)
            {
                (double X, double Y, double W, double H) cb = bounds.Value;
                minX = cb.X - 40;
                minY = cb.Y - 40;
                maxX = cb.X + cb.W + 40;
                maxY = cb.Y + cb.H + 40;
            }
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{F(minX)} {F(minY)} {F(maxX - minX)} {F(maxY - minY)}\" width=\"{F(maxX - minX)}\" height=\"{F(maxY - minY)}\" font-family=\"Segoe UI, Arial, sans-serif\">\n");
            sb.Append(defs);
            if (background)
            {
                sb.Append($"<rect x=\"{F(minX)}\" y=\"{F(minY)}\" width=\"{F(maxX - minX)}\" height=\"{F(maxY - minY)}\" fill=\"#ffffff\"/>\n");
            }
        }
        return closeContent;
    }

    /// <summary>A group: box, title band and title.</summary>
    static string GroupSvg(Group g)
    {
        StringBuilder sb = new StringBuilder();
        Swatch sw = Palette.Get(g.Color);
        string fill = "fill=\"none\"";
        if (g.Filled)
        {
            fill = $"fill=\"{sw.Light}\" fill-opacity=\"{F(g.FillOpacity())}\"";
        }
        double width = GroupLook.Width(g.BorderWeight);
        string? dash = LineLook.Dash(g.Border, width);
        string dashAttr = "";
        if (dash is not null)
        {
            dashAttr = $" stroke-dasharray=\"{dash}\"";
        }
        sb.Append($"<rect x=\"{F(g.X)}\" y=\"{F(g.Y)}\" width=\"{F(g.W)}\" height=\"{F(g.H)}\" rx=\"10\" {fill} stroke=\"{sw.Stroke}\" stroke-width=\"{F(width)}\"{dashAttr}/>\n");
        string? band = g.BandPath();
        if (band is not null)
        {
            sb.Append($"<path d=\"{band}\" fill=\"{sw.Stroke}\" fill-opacity=\"0.14\"/>\n");
        }
        (double tx, double ty, string anchor) = g.TitlePos();
        sb.Append($"<text x=\"{F(tx)}\" y=\"{F(ty)}\" font-size=\"13\" font-weight=\"600\" text-anchor=\"{anchor}\" fill=\"{sw.Stroke}\">{E(g.Title)}</text>\n");
        return sb.ToString();
    }

    /// <summary>A connector's line into <paramref name="lines"/>, and its label (if any) into <paramref name="labels"/>,
    /// which are written after the shapes so they sit on top.</summary>
    static void WriteEdge(StringBuilder lines, StringBuilder labels, Edge e, Dictionary<string, IBox> lookup)
    {
        (IBox A, IBox B)? ends = e.Ends(id => lookup.GetValueOrDefault(id));
        if (ends is null)
        {
            return;
        }
        (IBox a, IBox b) = ends.Value;
        EdgeGeom geo = EdgeRouter.EdgeGeometry(a, b, e);
        double lw = LineLook.Width(e.Weight);
        string lc = LineLook.Colour(e.Color);
        string? dash = LineLook.Dash(e.Pattern, lw);
        string mk = $"url(#{LineLook.MarkerId(lc, e.Weight)})";
        lines.Append($"<path d=\"{geo.D}\" fill=\"none\" stroke=\"{lc}\" stroke-width=\"{F(lw)}\" stroke-linecap=\"{LineLook.Cap(e.Pattern)}\"");
        if (dash is not null)
        {
            lines.Append($" stroke-dasharray=\"{dash}\"");
        }
        if (e.Arrows != LineLook.ArrowNone)
        {
            lines.Append($" marker-end=\"{mk}\"");
        }
        if (e.Arrows == LineLook.ArrowBoth)
        {
            lines.Append($" marker-start=\"{mk}\"");
        }
        lines.Append("/>\n");
        if (string.IsNullOrWhiteSpace(e.Label))
        {
            return;
        }
        EdgeLabelLayout lab = EdgeLabels.LayoutEdgeLabel(e, geo);
        labels.Append($"<g transform=\"translate({F(geo.LabelX)},{F(geo.LabelY)}) rotate({F(geo.LabelAngle)})\">");
        if (lab.Box is not null)
        {
            Rect bx = lab.Box;
            labels.Append($"<rect x=\"{F(bx.X)}\" y=\"{F(bx.Y)}\" width=\"{F(bx.W)}\" height=\"{F(bx.H)}\" rx=\"4\" fill=\"#fff\" stroke=\"#cfd4da\"/>");
        }
        string halo = "";
        if (lab.Halo)
        {
            halo = " stroke=\"#fff\" stroke-width=\"3\" stroke-linejoin=\"round\" paint-order=\"stroke\"";
        }
        labels.Append($"<text font-size=\"12\" text-anchor=\"{lab.Anchor}\" fill=\"#333\"{halo}>");
        foreach (EdgeLabelLine line in lab.Lines)
        {
            labels.Append($"<tspan x=\"{F(line.X)}\" y=\"{F(line.Y)}\">{E(line.Text)}</tspan>");
        }
        labels.Append("</text></g>\n");
    }

    /// <summary>A shape: halo, shadow, outline, side stripe, body, icon, label, description and badges.</summary>
    static void WriteNode(StringBuilder sb, Diagram d, Node n)
    {
        NodeStyle st = NodeStyle.For(n);
        IEmphasis emphasis = Emphases.Of(n);
        string shape = SvgShapes.ShapePath(n);
        string? haloSvg = SvgShapes.HaloSvg(n, st.Stroke);
        if (haloSvg is not null)
        {
            sb.Append(haloSvg).Append('\n');
        }
        if (emphasis.ShadowOpacity > 0)
        {
            sb.Append($"<path d=\"{shape}\" transform=\"{SvgShapes.ShadowTransform}\" fill=\"{st.Stroke}\" fill-opacity=\"{F(emphasis.ShadowOpacity)}\"/>\n");
        }
        sb.Append($"<path d=\"{shape}\" fill=\"{st.Fill}\" stroke=\"{st.Stroke}\" stroke-width=\"{F(emphasis.StrokeWidth)}\"/>\n");
        if (n.Kind.OutlineOf(n).HasSideStripe)
        {
            string stripeFill = st.Stroke;
            string stripeOpacity = "1";
            if (n.Filled)
            {
                stripeFill = "#ffffff";
                stripeOpacity = "0.5";
            }
            sb.Append($"<rect x=\"{F(n.X)}\" y=\"{F(n.Y)}\" width=\"6\" height=\"{F(n.H)}\" fill=\"{stripeFill}\" fill-opacity=\"{stripeOpacity}\"/>");
        }
        string? body = n.Kind.Body(n, st);
        if (body is not null)
        {
            sb.Append(body).Append('\n');
        }
        LabelLayout lay = TextLayout.Layout(n);
        if (lay.HasIcon)
        {
            sb.Append($"<g transform=\"translate({F(lay.IconX)},{F(lay.IconY)}) scale({F(lay.IconSize / 24)})\" fill=\"none\" stroke=\"{st.IconColor}\" stroke-width=\"{F(n.Kind.IconStroke)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Icons.Get(n.Kind.IconOf(n))}</g>\n");
        }
        (double X, double Y, double W, double H)? textBack = TextLayout.DrawnTextBack(n, lay);
        if (textBack is not null)
        {
            (double X, double Y, double W, double H) back = textBack.Value;
            sb.Append($"<rect x=\"{F(back.X)}\" y=\"{F(back.Y)}\" width=\"{F(back.W)}\" height=\"{F(back.H)}\" rx=\"4\" fill=\"#ffffff\" fill-opacity=\"0.92\"/>");
        }
        string weight = "";
        string? labelWeight = emphasis.LabelWeight(n);
        if (labelWeight is not null)
        {
            weight = $" font-weight=\"{labelWeight}\"";
        }
        string labelColour = st.Text;
        if (lay.Below)
        {
            labelColour = emphasis.BelowLabelColour;
        }
        sb.Append($"<text x=\"{F(lay.TextX)}\" y=\"{F(lay.TextY)}\" font-size=\"13\"{weight} text-anchor=\"middle\" fill=\"{labelColour}\">");
        for (int i = 0; i < lay.Lines.Count; i++)
        {
            string dy = "16";
            if (i == 0)
            {
                dy = "0";
            }
            sb.Append($"<tspan x=\"{F(lay.TextX)}\" dy=\"{dy}\">{E(lay.Lines[i])}</tspan>");
        }
        sb.Append("</text>\n");
        if (lay.Desc.Count > 0)
        {
            sb.Append($"<text x=\"{F(lay.DescX)}\" y=\"{F(lay.DescY)}\" font-size=\"{F(TextLayout.DescFontSize)}\" text-anchor=\"middle\" fill=\"#55606e\">");
            for (int i = 0; i < lay.Desc.Count; i++)
            {
                string dy = F(TextLayout.DescLineHeight);
                if (i == 0)
                {
                    dy = "0";
                }
                sb.Append($"<tspan x=\"{F(lay.DescX)}\" dy=\"{dy}\">{E(lay.Desc[i])}</tspan>");
            }
            sb.Append("</text>\n");
        }
        if (SvgShapes.HasLink(n))
        {
            sb.Append(SvgShapes.LinkBadgeSvg(n, st.Stroke)).Append('\n');
        }
        int? step = SvgShapes.ShownStep(d, n);
        if (step is not null)
        {
            sb.Append(SvgShapes.StepBadgeSvg(n, step.Value, st.Stroke)).Append('\n');
        }
    }

    /// <summary>A text box: background, border, folded corner (comments) and wrapped lines.</summary>
    static string TextBoxSvg(TextBox t)
    {
        TextStyle st = TextStyle.For(t);
        StringBuilder sb = new StringBuilder("<g>");
        if (st.Fill is not null || st.Stroke is not null)
        {
            sb.Append($"<path d=\"{SvgShapes.TextBoxPath(t, st.Radius)}\" fill=\"{st.Fill ?? "none"}\"");
            if (st.Stroke is not null)
            {
                sb.Append($" stroke=\"{st.Stroke}\" stroke-width=\"{F(st.StrokeWidth)}\" stroke-linecap=\"{st.Cap}\"");
                if (st.Dash is not null) sb.Append($" stroke-dasharray=\"{st.Dash}\"");
            }
            sb.Append("/>");
        }
        if (t.IsComment) sb.Append($"<path d=\"{SvgShapes.FoldPath(t)}\" fill=\"none\" stroke=\"{st.FoldColour}\"/>");
        TextBoxLayout lay = TextLayout.Layout(t);
        sb.Append($"<text font-family=\"{E(st.Family)}\" font-size=\"{F(t.FontSize)}\"{(t.Bold ? " font-weight=\"600\"" : "")} fill=\"{st.TextColour}\" text-anchor=\"{lay.Anchor}\" xml:space=\"preserve\">");
        foreach (TextLine l in lay.Lines)
            sb.Append($"<tspan x=\"{F(l.X)}\" y=\"{F(l.Y)}\">{E(l.Text)}</tspan>");
        sb.Append("</text></g>\n");
        return sb.ToString();
    }
}
