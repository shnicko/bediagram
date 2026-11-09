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
        List<IBox> boxes = d.Nodes.Cast<IBox>().Concat(d.TextBoxes).Concat(d.Groups).ToList();
        Dictionary<string, IBox> lookup = boxes.ToDictionary(b => b.Id);
        StringBuilder sb = new StringBuilder();
        string defs = "<defs>" + string.Concat(d.Edges.Select(x => (LineLook.Colour(x.Color), x.Weight)).Distinct()
            .Select(m => LineLook.MarkerDef(m.Item1, m.Weight))) + "</defs>\n";
        string closeContent = "";

        if (view is not null)
        {
            if (d.Page.Enabled)
            {
                (double wmm, double hmm) = d.Page.Mm;
                double W = d.Page.WidthPx, H = d.Page.HeightPx, m = d.Page.MarginPx;
                double availW = Math.Max(1, W - 2 * m), availH = Math.Max(1, H - 2 * m);
                double scale = Math.Min(availW / view.W, availH / view.H);
                double tx = m + (availW - view.W * scale) / 2 - view.X * scale;
                double ty = m + (availH - view.H * scale) / 2 - view.Y * scale;
                sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(wmm)}mm\" height=\"{F(hmm)}mm\" viewBox=\"0 0 {F(W)} {F(H)}\" font-family=\"Segoe UI, Arial, sans-serif\">\n");
                sb.Append(defs.Replace("</defs>", $"<clipPath id=\"view\"><rect x=\"{F(view.X)}\" y=\"{F(view.Y)}\" width=\"{F(view.W)}\" height=\"{F(view.H)}\"/></clipPath></defs>"));
                if (background) sb.Append($"<rect x=\"0\" y=\"0\" width=\"{F(W)}\" height=\"{F(H)}\" fill=\"#ffffff\"/>\n");
                sb.Append($"<g transform=\"translate({F(tx)},{F(ty)}) scale({F(scale)})\"><g clip-path=\"url(#view)\">\n");
                closeContent = "</g></g>\n";
            }
            else
            {
                // Exactly the screen: the viewBox does the clipping.
                sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{F(view.X)} {F(view.Y)} {F(view.W)} {F(view.H)}\" width=\"{F(view.ScreenW)}\" height=\"{F(view.ScreenH)}\" font-family=\"Segoe UI, Arial, sans-serif\">\n");
                sb.Append(defs);
                if (background) sb.Append($"<rect x=\"{F(view.X)}\" y=\"{F(view.Y)}\" width=\"{F(view.W)}\" height=\"{F(view.H)}\" fill=\"#ffffff\"/>\n");
            }
        }
        else if (d.Page.Enabled)
        {
            // Fixed paper size: physical mm dimensions so it drops into Word/PowerPoint at the right size.
            (double wmm, double hmm) = d.Page.Mm;
            double W = d.Page.WidthPx, H = d.Page.HeightPx, m = d.Page.MarginPx;
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(wmm)}mm\" height=\"{F(hmm)}mm\" viewBox=\"0 0 {F(W)} {F(H)}\" font-family=\"Segoe UI, Arial, sans-serif\">\n");
            sb.Append(defs);
            if (background) sb.Append($"<rect x=\"0\" y=\"0\" width=\"{F(W)}\" height=\"{F(H)}\" fill=\"#ffffff\"/>\n");
            if (Boxes.ContentBounds(d) is { } cb && d.Page.FitOnExport)
            {
                double availW = W - 2 * m, availH = H - 2 * m;
                bool fits = cb.X >= m - 0.5 && cb.Y >= m - 0.5 && cb.X + cb.W <= W - m + 0.5 && cb.Y + cb.H <= H - m + 0.5;
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
            double minX = 0, minY = 0, maxX = 400, maxY = 300;
            if (Boxes.ContentBounds(d) is { } cb)
            {
                minX = cb.X - 40; minY = cb.Y - 40; maxX = cb.X + cb.W + 40; maxY = cb.Y + cb.H + 40;
            }
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{F(minX)} {F(minY)} {F(maxX - minX)} {F(maxY - minY)}\" width=\"{F(maxX - minX)}\" height=\"{F(maxY - minY)}\" font-family=\"Segoe UI, Arial, sans-serif\">\n");
            sb.Append(defs);
            if (background) sb.Append($"<rect x=\"{F(minX)}\" y=\"{F(minY)}\" width=\"{F(maxX - minX)}\" height=\"{F(maxY - minY)}\" fill=\"#ffffff\"/>\n");
        }

        // Drawing order: a group inside another is drawn over it.
        foreach (Group g in new GroupTree(d).PaintOrder())
        {
            Swatch sw = Palette.Get(g.Color);
            string fill = g.Filled ? $"fill=\"{sw.Light}\" fill-opacity=\"{F(g.FillOpacity())}\"" : "fill=\"none\"";
            double width = GroupLook.Width(g.BorderWeight);
            string? dash = LineLook.Dash(g.Border, width);
            sb.Append($"<rect x=\"{F(g.X)}\" y=\"{F(g.Y)}\" width=\"{F(g.W)}\" height=\"{F(g.H)}\" rx=\"10\" {fill} stroke=\"{sw.Stroke}\" stroke-width=\"{F(width)}\"{(dash is null ? "" : $" stroke-dasharray=\"{dash}\"")}/>\n");
            if (g.BandPath() is { } band)
                sb.Append($"<path d=\"{band}\" fill=\"{sw.Stroke}\" fill-opacity=\"0.14\"/>\n");
            (double tx, double ty, string anchor) = g.TitlePos();
            sb.Append($"<text x=\"{F(tx)}\" y=\"{F(ty)}\" font-size=\"13\" font-weight=\"600\" text-anchor=\"{anchor}\" fill=\"{sw.Stroke}\">{E(g.Title)}</text>\n");
        }

        foreach (TextBox t in d.TextBoxes)
            sb.Append(TextBoxSvg(t));

        StringBuilder labels = new StringBuilder();
        // Connectors ending on a row of a list / IPO node go above the shapes, so that end isn't hidden.
        StringBuilder rowEdges = new StringBuilder();
        foreach (Edge e in d.Edges)
        {
            StringBuilder lines = e.EndsOnRow ? rowEdges : sb;
            if (e.Ends(id => lookup.GetValueOrDefault(id)) is not { } ends) continue;
            (IBox a, IBox b) = ends;
            EdgeGeom geo = EdgeRouter.EdgeGeometry(a, b, e);
            double lw = LineLook.Width(e.Weight);
            string lc = LineLook.Colour(e.Color);
            string? dash = LineLook.Dash(e.Pattern, lw);
            string mk = $"url(#{LineLook.MarkerId(lc, e.Weight)})";
            lines.Append($"<path d=\"{geo.D}\" fill=\"none\" stroke=\"{lc}\" stroke-width=\"{F(lw)}\" stroke-linecap=\"{LineLook.Cap(e.Pattern)}\"");
            if (dash is not null) lines.Append($" stroke-dasharray=\"{dash}\"");
            if (e.Arrows != LineLook.ArrowNone) lines.Append($" marker-end=\"{mk}\"");
            if (e.Arrows == LineLook.ArrowBoth) lines.Append($" marker-start=\"{mk}\"");
            lines.Append("/>\n");
            if (!string.IsNullOrWhiteSpace(e.Label))
            {
                EdgeLabelLayout lab = EdgeLabels.LayoutEdgeLabel(e, geo);
                // Labels are written after the shapes so they sit on top.
                labels.Append($"<g transform=\"translate({F(geo.LabelX)},{F(geo.LabelY)}) rotate({F(geo.LabelAngle)})\">");
                if (lab.Box is { } bx)
                    labels.Append($"<rect x=\"{F(bx.X)}\" y=\"{F(bx.Y)}\" width=\"{F(bx.W)}\" height=\"{F(bx.H)}\" rx=\"4\" fill=\"#fff\" stroke=\"#cfd4da\"/>");
                string halo = lab.Halo ? " stroke=\"#fff\" stroke-width=\"3\" stroke-linejoin=\"round\" paint-order=\"stroke\"" : "";
                labels.Append($"<text font-size=\"12\" text-anchor=\"{lab.Anchor}\" fill=\"#333\"{halo}>");
                foreach (EdgeLabelLine line in lab.Lines)
                    labels.Append($"<tspan x=\"{F(line.X)}\" y=\"{F(line.Y)}\">{E(line.Text)}</tspan>");
                labels.Append("</text></g>\n");
            }
        }

        foreach (Node n in d.Nodes)
        {
            NodeStyle st = NodeStyle.For(n);
            string shape = SvgShapes.ShapePath(n);
            if (SvgShapes.HaloSvg(n, st.Stroke) is { } halo) sb.Append(halo).Append('\n');
            if (Emphases.Of(n).ShadowOpacity > 0)
                sb.Append($"<path d=\"{shape}\" transform=\"{SvgShapes.ShadowTransform}\" fill=\"{st.Stroke}\" fill-opacity=\"{F(Emphases.Of(n).ShadowOpacity)}\"/>\n");
            sb.Append($"<path d=\"{shape}\" fill=\"{st.Fill}\" stroke=\"{st.Stroke}\" stroke-width=\"{F(Emphases.Of(n).StrokeWidth)}\"/>\n");
            if (n.Kind.OutlineOf(n).HasSideStripe)
                sb.Append($"<rect x=\"{F(n.X)}\" y=\"{F(n.Y)}\" width=\"6\" height=\"{F(n.H)}\" fill=\"{(n.Filled ? "#ffffff" : st.Stroke)}\" fill-opacity=\"{(n.Filled ? "0.5" : "1")}\"/>");
            if (n.Kind.Body(n, st) is { } body) sb.Append(body).Append('\n');
            LabelLayout lay = TextLayout.Layout(n);
            if (lay.HasIcon)
                sb.Append($"<g transform=\"translate({F(lay.IconX)},{F(lay.IconY)}) scale({F(lay.IconSize / 24)})\" fill=\"none\" stroke=\"{st.IconColor}\" stroke-width=\"{F(n.Kind.IconStroke)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Icons.Get(n.Kind.IconOf(n))}</g>\n");
            if (TextLayout.DrawnTextBack(n, lay) is { } back)
                sb.Append($"<rect x=\"{F(back.X)}\" y=\"{F(back.Y)}\" width=\"{F(back.W)}\" height=\"{F(back.H)}\" rx=\"4\" fill=\"#ffffff\" fill-opacity=\"0.92\"/>");
            string weight = Emphases.Of(n).LabelWeight(n) is { } lw ? $" font-weight=\"{lw}\"" : "";
            sb.Append($"<text x=\"{F(lay.TextX)}\" y=\"{F(lay.TextY)}\" font-size=\"13\"{weight} text-anchor=\"middle\" fill=\"{(lay.Below ? Emphases.Of(n).BelowLabelColour : st.Text)}\">");
            for (int i = 0; i < lay.Lines.Count; i++)
                sb.Append($"<tspan x=\"{F(lay.TextX)}\" dy=\"{(i == 0 ? "0" : "16")}\">{E(lay.Lines[i])}</tspan>");
            sb.Append("</text>\n");
            if (lay.Desc.Count > 0)
            {
                sb.Append($"<text x=\"{F(lay.DescX)}\" y=\"{F(lay.DescY)}\" font-size=\"{F(TextLayout.DescFontSize)}\" text-anchor=\"middle\" fill=\"#55606e\">");
                for (int i = 0; i < lay.Desc.Count; i++)
                    sb.Append($"<tspan x=\"{F(lay.DescX)}\" dy=\"{(i == 0 ? "0" : F(TextLayout.DescLineHeight))}\">{E(lay.Desc[i])}</tspan>");
                sb.Append("</text>\n");
            }
            if (SvgShapes.HasLink(n)) sb.Append(SvgShapes.LinkBadgeSvg(n, st.Stroke)).Append('\n');
            if (SvgShapes.ShownStep(d, n) is int step) sb.Append(SvgShapes.StepBadgeSvg(n, step, st.Stroke)).Append('\n');
        }
        sb.Append(rowEdges);
        // Group link badges sit above the members so they're never hidden.
        foreach (Group g in d.Groups.Where(SvgShapes.HasLink))
            sb.Append(SvgShapes.LinkBadgeSvg(g, Palette.Get(g.Color).Stroke)).Append('\n');
        sb.Append(labels);
        sb.Append(closeContent);
        sb.Append("</svg>\n");
        return sb.ToString();
    }

    /// <summary>A text box: background, border, folded corner (comments) and wrapped lines.</summary>
    string TextBoxSvg(TextBox t)
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
