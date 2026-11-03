using System.Globalization;
using System.Net;
using System.Text;
using BeDiagram.Models;

namespace BeDiagram.Services;

/// <summary>
/// A window onto the drawing in world coordinates (what's on screen) plus the on-screen size in px.
/// Exported as-is on an open canvas, or scaled to fit inside the margins when a page size is set.
/// </summary>
public record ExportView(double X, double Y, double W, double H, double ScreenW, double ScreenH);

public static class Exporters
{
    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    static string E(string s) => WebUtility.HtmlEncode(s ?? "");

    /// <summary>
    /// Standalone SVG of the diagram. Without a view: the page (when a size is set) or the drawing extents.
    /// With a view: just that window, clipped — at screen size, or fitted to the page when a size is set.
    /// </summary>
    public static string ToSvg(Diagram d, ExportView? view = null)
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
                sb.Append($"<rect x=\"0\" y=\"0\" width=\"{F(W)}\" height=\"{F(H)}\" fill=\"#ffffff\"/>\n");
                sb.Append($"<g transform=\"translate({F(tx)},{F(ty)}) scale({F(scale)})\"><g clip-path=\"url(#view)\">\n");
                closeContent = "</g></g>\n";
            }
            else
            {
                // Exactly the screen: the viewBox does the clipping.
                sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{F(view.X)} {F(view.Y)} {F(view.W)} {F(view.H)}\" width=\"{F(view.ScreenW)}\" height=\"{F(view.ScreenH)}\" font-family=\"Segoe UI, Arial, sans-serif\">\n");
                sb.Append(defs);
                sb.Append($"<rect x=\"{F(view.X)}\" y=\"{F(view.Y)}\" width=\"{F(view.W)}\" height=\"{F(view.H)}\" fill=\"#ffffff\"/>\n");
            }
        }
        else if (d.Page.Enabled)
        {
            // Fixed paper size: physical mm dimensions so it drops into Word/PowerPoint at the right size.
            (double wmm, double hmm) = d.Page.Mm;
            double W = d.Page.WidthPx, H = d.Page.HeightPx, m = d.Page.MarginPx;
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{F(wmm)}mm\" height=\"{F(hmm)}mm\" viewBox=\"0 0 {F(W)} {F(H)}\" font-family=\"Segoe UI, Arial, sans-serif\">\n");
            sb.Append(defs);
            sb.Append($"<rect x=\"0\" y=\"0\" width=\"{F(W)}\" height=\"{F(H)}\" fill=\"#ffffff\"/>\n");
            if (Geometry.ContentBounds(d) is { } cb && d.Page.FitOnExport)
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
            if (Geometry.ContentBounds(d) is { } cb)
            {
                minX = cb.X - 40; minY = cb.Y - 40; maxX = cb.X + cb.W + 40; maxY = cb.Y + cb.H + 40;
            }
            sb.Append($"<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"{F(minX)} {F(minY)} {F(maxX - minX)} {F(maxY - minY)}\" width=\"{F(maxX - minX)}\" height=\"{F(maxY - minY)}\" font-family=\"Segoe UI, Arial, sans-serif\">\n");
            sb.Append(defs);
            sb.Append($"<rect x=\"{F(minX)}\" y=\"{F(minY)}\" width=\"{F(maxX - minX)}\" height=\"{F(maxY - minY)}\" fill=\"#ffffff\"/>\n");
        }

        foreach (Group g in d.Groups)
        {
            Palette.Swatch sw = Palette.Get(g.Color);
            sb.Append($"<rect x=\"{F(g.X)}\" y=\"{F(g.Y)}\" width=\"{F(g.W)}\" height=\"{F(g.H)}\" rx=\"10\" fill=\"{sw.Light}\" fill-opacity=\"0.6\" stroke=\"{sw.Stroke}\" stroke-width=\"1.5\"/>\n");
            sb.Append($"<path d=\"M{F(g.X)},{F(g.Y + Group.Header)} V{F(g.Y + 10)} Q{F(g.X)},{F(g.Y)} {F(g.X + 10)},{F(g.Y)} H{F(g.X + g.W - 10)} Q{F(g.X + g.W)},{F(g.Y)} {F(g.X + g.W)},{F(g.Y + 10)} V{F(g.Y + Group.Header)} Z\" fill=\"{sw.Stroke}\" fill-opacity=\"0.14\"/>\n");
            sb.Append($"<text x=\"{F(g.X + 14)}\" y=\"{F(g.Y + 21)}\" font-size=\"13\" font-weight=\"600\" fill=\"{sw.Stroke}\">{E(g.Title)}</text>\n");
        }

        foreach (TextBox t in d.TextBoxes)
            sb.Append(TextBoxSvg(t));

        StringBuilder labels = new StringBuilder();
        foreach (Edge e in d.Edges)
        {
            if (!lookup.TryGetValue(e.From, out IBox? a) || !lookup.TryGetValue(e.To, out IBox? b)) continue;
            Geometry.EdgeGeom geo = Geometry.EdgeGeometry(a, b, e);
            double lw = LineLook.Width(e.Weight);
            string lc = LineLook.Colour(e.Color);
            string? dash = LineLook.Dash(e.Pattern, lw);
            string mk = $"url(#{LineLook.MarkerId(lc, e.Weight)})";
            sb.Append($"<path d=\"{geo.D}\" fill=\"none\" stroke=\"{lc}\" stroke-width=\"{F(lw)}\" stroke-linecap=\"{LineLook.Cap(e.Pattern)}\"");
            if (dash is not null) sb.Append($" stroke-dasharray=\"{dash}\"");
            if (e.Arrows != LineLook.ArrowNone) sb.Append($" marker-end=\"{mk}\"");
            if (e.Arrows == LineLook.ArrowBoth) sb.Append($" marker-start=\"{mk}\"");
            sb.Append("/>\n");
            if (!string.IsNullOrWhiteSpace(e.Label))
            {
                double w = Geometry.LabelWidth(e.Label);
                // Labels are written after the shapes so they sit on top.
                labels.Append($"<g transform=\"translate({F(geo.LabelX)},{F(geo.LabelY)}) rotate({F(geo.LabelAngle)})\">");
                labels.Append($"<rect x=\"{F(-w / 2)}\" y=\"-10\" width=\"{F(w)}\" height=\"20\" rx=\"4\" fill=\"#fff\" stroke=\"#cfd4da\"/>");
                labels.Append($"<text x=\"0\" y=\"4\" font-size=\"12\" text-anchor=\"middle\" fill=\"#333\">{E(e.Label)}</text></g>\n");
            }
        }

        foreach (Node n in d.Nodes)
        {
            NodeTypeInfo t = NodeTypes.Get(n.Type);
            NodeStyle st = NodeStyle.For(n);
            sb.Append($"<path d=\"{Geometry.ShapePath(n)}\" fill=\"{st.Fill}\" stroke=\"{st.Stroke}\" stroke-width=\"1.6\"/>\n");
            if (t.Shape == "check")
                sb.Append($"<rect x=\"{F(n.X)}\" y=\"{F(n.Y)}\" width=\"6\" height=\"{F(n.H)}\" fill=\"{(n.Filled ? "#ffffff" : st.Stroke)}\" fill-opacity=\"{(n.Filled ? "0.5" : "1")}\"/>");
            Geometry.LabelLayout lay = Geometry.Layout(n, string.IsNullOrWhiteSpace(n.Label) ? t.Display : n.Label);
            if (lay.HasIcon)
                sb.Append($"<g transform=\"translate({F(lay.IconX)},{F(lay.IconY)}) scale({F(lay.IconSize / 24)})\" fill=\"none\" stroke=\"{st.IconColor}\" stroke-width=\"{F(Geometry.IconStroke(n))}\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Icons.Get(Geometry.IconOf(n))}</g>\n");
            if (lay.Below)
            {
                double bw = lay.Lines.Max(l => l.Length) * 7.2 + 14;
                sb.Append($"<rect x=\"{F(lay.TextX - bw / 2)}\" y=\"{F(lay.TextY - 14)}\" width=\"{F(bw)}\" height=\"{F(lay.Lines.Count * Geometry.LineHeight + 6)}\" rx=\"4\" fill=\"#ffffff\" fill-opacity=\"0.92\"/>");
            }
            sb.Append($"<text x=\"{F(lay.TextX)}\" y=\"{F(lay.TextY)}\" font-size=\"13\" text-anchor=\"middle\" fill=\"{(lay.Below ? "#1f2933" : st.Text)}\">");
            for (int i = 0; i < lay.Lines.Count; i++)
                sb.Append($"<tspan x=\"{F(lay.TextX)}\" dy=\"{(i == 0 ? "0" : "16")}\">{E(lay.Lines[i])}</tspan>");
            sb.Append("</text>\n");
            if (Geometry.HasLink(n)) sb.Append(Geometry.LinkBadgeSvg(n, st.Stroke)).Append('\n');
        }
        // Group link badges sit above the members so they're never hidden.
        foreach (Group g in d.Groups.Where(Geometry.HasLink))
            sb.Append(Geometry.LinkBadgeSvg(g, Palette.Get(g.Color).Stroke)).Append('\n');
        sb.Append(labels);
        sb.Append(closeContent);
        sb.Append("</svg>\n");
        return sb.ToString();
    }

    /// <summary>A text box: background, border, folded corner (comments) and wrapped lines.</summary>
    static string TextBoxSvg(TextBox t)
    {
        TextStyle st = TextStyle.For(t);
        StringBuilder sb = new StringBuilder("<g>");
        if (st.Fill is not null || st.Stroke is not null)
        {
            sb.Append($"<path d=\"{Geometry.TextBoxPath(t, st.Radius)}\" fill=\"{st.Fill ?? "none"}\"");
            if (st.Stroke is not null)
            {
                sb.Append($" stroke=\"{st.Stroke}\" stroke-width=\"{F(st.StrokeWidth)}\" stroke-linecap=\"{st.Cap}\"");
                if (st.Dash is not null) sb.Append($" stroke-dasharray=\"{st.Dash}\"");
            }
            sb.Append("/>");
        }
        if (t.IsComment) sb.Append($"<path d=\"{Geometry.FoldPath(t)}\" fill=\"none\" stroke=\"{st.FoldColour}\"/>");
        Geometry.TextBoxLayout lay = Geometry.Layout(t);
        sb.Append($"<text font-family=\"{E(st.Family)}\" font-size=\"{F(t.FontSize)}\"{(t.Bold ? " font-weight=\"600\"" : "")} fill=\"{st.TextColour}\" text-anchor=\"{lay.Anchor}\" xml:space=\"preserve\">");
        foreach (Geometry.TextLine l in lay.Lines)
            sb.Append($"<tspan x=\"{F(l.X)}\" y=\"{F(l.Y)}\">{E(l.Text)}</tspan>");
        sb.Append("</text></g>\n");
        return sb.ToString();
    }

    /// <summary>Mermaid link syntax: dotted for any broken pattern, thick for heavy solid lines.</summary>
    static string MermaidLink(Edge e)
    {
        static string Q(string s) => "\"" + (s ?? "").Replace("\"", "'").Replace("\r", "").Replace("\n", " ").Trim() + "\"";
        bool label = !string.IsNullOrWhiteSpace(e.Label);
        bool dotted = e.Pattern != LineLook.Solid;
        bool thick = !dotted && e.Weight == LineLook.Heavy;
        string start = e.Arrows == LineLook.ArrowBoth ? "<" : "";
        string end = e.Arrows == LineLook.ArrowNone ? "" : ">";
        if (dotted)
            return label ? $"{start}-. {Q(e.Label)} .-{end}" : $"{start}-.-{end}";
        if (thick)
            return label ? $"{start}== {Q(e.Label)} =={(end == "" ? "=" : ">")}" : $"{start}=={(end == "" ? "=" : ">")}";
        return label ? $"{start}-- {Q(e.Label)} --{(end == "" ? "-" : ">")}" : $"{start}--{(end == "" ? "-" : ">")}";
    }

    /// <summary>Mermaid flowchart text, for interoperability with Mermaid-based tools.</summary>
    public static string ToMermaid(Diagram d)
    {
        static string Q(string s) => "\"" + (s ?? "").Replace("\"", "'").Replace("\r", "").Replace("\n", " ").Trim() + "\"";
        StringBuilder sb = new StringBuilder("flowchart TD\n");
        Dictionary<string, string> ids = new Dictionary<string, string>();
        StringBuilder styles = new StringBuilder();
        int i = 0;
        foreach (Node n in d.Nodes)
        {
            string id = $"N{++i}";
            ids[n.Id] = id;
            string text = string.IsNullOrWhiteSpace(n.Label) ? NodeTypes.Get(n.Type).Display : n.Label;
            if (n.Type == NodeTypes.Outcome)
            {
                // Mermaid has no seal/octagon: a symbol in the label plus the closest classic shape and a solid fill.
                OutcomeInfo o = Outcomes.Get(n.Outcome);
                (string sym, string open, string close) = o.Key switch
                {
                    Outcomes.Fail => ("✕", "{{", "}}"),
                    Outcomes.Rework => ("↺", "((", "))"),
                    Outcomes.Conditional => ("⚠", "[/", "\\]"),
                    Outcomes.Hold => ("⏸", "(", ")"),
                    _ => ("✓", "((", "))"),
                };
                sb.Append($"    {id}{open}{Q($"{sym} {text}")}{close}\n");
                NodeStyle ost = NodeStyle.For(n);
                styles.Append($"    style {id} fill:{ost.Fill},stroke:{ost.Stroke},color:{(n.Filled ? ost.IconColor : "#1f2933")}\n");
                continue;
            }
            string label = Q(text);
            string shape = NodeTypes.Get(n.Type).Shape switch
            {
                "pill" => $"([{label}])",
                "circle" => $"(({label}))",
                "diamond" => $"{{{label}}}",
                "parallelogram" => $"[/{label}/]",
                "hexagon" => $"{{{{{label}}}}}",
                "rect-rounded" => $"({label})",
                _ => $"[{label}]",
            };
            sb.Append($"    {id}{shape}\n");
        }
        int ti = 0;
        foreach (TextBox t in d.TextBoxes)
        {
            string id = $"T{++ti}";
            ids[t.Id] = id;
            string text = (t.Text ?? "").Replace("\"", "'").Replace("\r", "").Trim().Replace("\n", "<br/>");
            sb.Append($"    {id}[\"{(text.Length == 0 ? " " : text)}\"]\n");
            TextStyle st = TextStyle.For(t);
            List<string> css = new List<string> { $"fill:{st.Fill ?? "transparent"}", $"color:{st.TextColour}" };
            if (st.Stroke is null) css.Add("stroke-width:0px");
            else
            {
                css.Add($"stroke:{st.Stroke}");
                css.Add($"stroke-width:{F(st.StrokeWidth)}px");
                if (st.Dash is not null) css.Add($"stroke-dasharray:{st.Dash}");
            }
            styles.Append($"    style {id} {string.Join(",", css)}\n");
        }
        int gi = 0;
        foreach (Group g in d.Groups)
        {
            string gid = $"G{++gi}";
            ids[g.Id] = gid;
            sb.Append($"    subgraph {gid}[{Q(g.Title)}]\n");
            foreach (IGroupable m in d.Nodes.Cast<IGroupable>().Concat(d.TextBoxes).Where(m => m.GroupId == g.Id))
                sb.Append($"        {ids[m.Id]}\n");
            sb.Append("    end\n");
        }
        foreach (Edge e in d.Edges)
        {
            if (!ids.TryGetValue(e.From, out string? a) || !ids.TryGetValue(e.To, out string? b)) continue;
            sb.Append($"    {a} {MermaidLink(e)} {b}\n");
        }
        // Mermaid has no notion of a linked sub-diagram; keep the links as comments so they aren't lost.
        foreach ((string id, string? link) in d.Nodes.Select(n => (n.Id, n.Link)).Concat(d.Groups.Select(g => (g.Id, g.Link))))
            if (!string.IsNullOrWhiteSpace(link) && ids.TryGetValue(id, out string? mid))
                sb.Append($"    %% {mid} links to diagram {Q(link)}\n");
        sb.Append(styles);
        return sb.ToString();
    }
}
