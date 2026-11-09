using System.Globalization;
using System.Net;
using System.Text;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;

namespace BeDiagram.Services;

public static class Exporters
{
    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
    static string E(string s) => WebUtility.HtmlEncode(s ?? "");

    /// <summary>
    /// Standalone SVG of the diagram. Without a view: the page (when a size is set) or the drawing extents.
    /// With a view: just that window, clipped — at screen size, or fitted to the page when a size is set.
    /// <paramref name="background"/> = false leaves out the white background (transparent PNG export).
    /// </summary>
    public static string ToSvg(Diagram d, ExportView? view = null, bool background = true)
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
            if (background) sb.Append($"<rect x=\"{F(minX)}\" y=\"{F(minY)}\" width=\"{F(maxX - minX)}\" height=\"{F(maxY - minY)}\" fill=\"#ffffff\"/>\n");
        }

        // Drawing order: a group inside another is drawn over it.
        foreach (Group g in new GroupTree(d).PaintOrder())
        {
            Swatch sw = Palette.Get(g.Color);
            string fill = g.Filled ? $"fill=\"{sw.Light}\" fill-opacity=\"{F(Geometry.GroupFillOpacity(g))}\"" : "fill=\"none\"";
            double width = GroupLook.Width(g.BorderWeight);
            string? dash = LineLook.Dash(g.Border, width);
            sb.Append($"<rect x=\"{F(g.X)}\" y=\"{F(g.Y)}\" width=\"{F(g.W)}\" height=\"{F(g.H)}\" rx=\"10\" {fill} stroke=\"{sw.Stroke}\" stroke-width=\"{F(width)}\"{(dash is null ? "" : $" stroke-dasharray=\"{dash}\"")}/>\n");
            if (Geometry.GroupBandPath(g) is { } band)
                sb.Append($"<path d=\"{band}\" fill=\"{sw.Stroke}\" fill-opacity=\"0.14\"/>\n");
            (double tx, double ty, string anchor) = Geometry.GroupTitlePos(g);
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
            if (PanelGeometry.Ends(e, id => lookup.GetValueOrDefault(id)) is not { } ends) continue;
            (IBox a, IBox b) = ends;
            EdgeGeom geo = Geometry.EdgeGeometry(a, b, e);
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
                EdgeLabelLayout lab = Geometry.LayoutEdgeLabel(e, geo);
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
            string shape = Geometry.ShapePath(n);
            if (Geometry.HaloSvg(n, st.Stroke) is { } halo) sb.Append(halo).Append('\n');
            if (Geometry.ShadowOpacityOf(n) > 0)
                sb.Append($"<path d=\"{shape}\" transform=\"{Geometry.ShadowTransform}\" fill=\"{st.Stroke}\" fill-opacity=\"{F(Geometry.ShadowOpacityOf(n))}\"/>\n");
            sb.Append($"<path d=\"{shape}\" fill=\"{st.Fill}\" stroke=\"{st.Stroke}\" stroke-width=\"{F(Geometry.ShapeStrokeOf(n))}\"/>\n");
            if (Geometry.OutlineOf(n).HasSideStripe)
                sb.Append($"<rect x=\"{F(n.X)}\" y=\"{F(n.Y)}\" width=\"6\" height=\"{F(n.H)}\" fill=\"{(n.Filled ? "#ffffff" : st.Stroke)}\" fill-opacity=\"{(n.Filled ? "0.5" : "1")}\"/>");
            if (n.Kind.IsPanel) sb.Append(PanelSvg.Body(n, st)).Append('\n');
            LabelLayout lay = Geometry.Layout(n);
            if (lay.HasIcon)
                sb.Append($"<g transform=\"translate({F(lay.IconX)},{F(lay.IconY)}) scale({F(lay.IconSize / 24)})\" fill=\"none\" stroke=\"{st.IconColor}\" stroke-width=\"{F(n.Kind.IconStroke)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{Icons.Get(n.Kind.IconOf(n))}</g>\n");
            if (Geometry.DrawnTextBack(n, lay) is { } back)
                sb.Append($"<rect x=\"{F(back.X)}\" y=\"{F(back.Y)}\" width=\"{F(back.W)}\" height=\"{F(back.H)}\" rx=\"4\" fill=\"#ffffff\" fill-opacity=\"0.92\"/>");
            string weight = Geometry.LabelWeight(n) is { } lw ? $" font-weight=\"{lw}\"" : "";
            sb.Append($"<text x=\"{F(lay.TextX)}\" y=\"{F(lay.TextY)}\" font-size=\"13\"{weight} text-anchor=\"middle\" fill=\"{(lay.Below ? Geometry.BelowLabelColour(n) : st.Text)}\">");
            for (int i = 0; i < lay.Lines.Count; i++)
                sb.Append($"<tspan x=\"{F(lay.TextX)}\" dy=\"{(i == 0 ? "0" : "16")}\">{E(lay.Lines[i])}</tspan>");
            sb.Append("</text>\n");
            if (lay.Desc.Count > 0)
            {
                sb.Append($"<text x=\"{F(lay.DescX)}\" y=\"{F(lay.DescY)}\" font-size=\"{F(Geometry.DescFontSize)}\" text-anchor=\"middle\" fill=\"#55606e\">");
                for (int i = 0; i < lay.Desc.Count; i++)
                    sb.Append($"<tspan x=\"{F(lay.DescX)}\" dy=\"{(i == 0 ? "0" : F(Geometry.DescLineHeight))}\">{E(lay.Desc[i])}</tspan>");
                sb.Append("</text>\n");
            }
            if (Geometry.HasLink(n)) sb.Append(Geometry.LinkBadgeSvg(n, st.Stroke)).Append('\n');
            if (Geometry.ShownStep(d, n) is int step) sb.Append(Geometry.StepBadgeSvg(n, step, st.Stroke)).Append('\n');
        }
        sb.Append(rowEdges);
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
        TextBoxLayout lay = Geometry.Layout(t);
        sb.Append($"<text font-family=\"{E(st.Family)}\" font-size=\"{F(t.FontSize)}\"{(t.Bold ? " font-weight=\"600\"" : "")} fill=\"{st.TextColour}\" text-anchor=\"{lay.Anchor}\" xml:space=\"preserve\">");
        foreach (TextLine l in lay.Lines)
            sb.Append($"<tspan x=\"{F(l.X)}\" y=\"{F(l.Y)}\">{E(l.Text)}</tspan>");
        sb.Append("</text></g>\n");
        return sb.ToString();
    }

    /// <summary>Mermaid link syntax: dotted for any broken pattern, thick for heavy solid lines.</summary>
    static string MermaidLink(Edge e)
    {
        static string Q(string s) => "\"" + string.Join("<br/>", Geometry.EdgeLabelLines((s ?? "").Replace("\"", "'"))) + "\"";
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

    /// <summary>Indents every line of a Mermaid block by <paramref name="depth"/> levels (4 spaces each).</summary>
    static string Indent(string block, int depth)
    {
        string pad = new string(' ', 4 * depth);
        return string.Concat(block.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => pad + l + "\n"));
    }

    /// <summary>
    /// A list / IPO node as Mermaid (not indented): a subgraph titled with <paramref name="title"/> (already quoted)
    /// holding a node per row (ids "N3_1", "N3_2", … recorded in <paramref name="rowIds"/>); an IPO box nests a
    /// subgraph per section that has rows. A panel with no rows at all is a plain node.
    /// </summary>
    static string PanelMermaid(Node n, string id, string title, Dictionary<(string Node, string Item), string> rowIds)
    {
        static string Row(ListItem item)
        {
            string label = (string.IsNullOrWhiteSpace(item.Label) ? "Item" : item.Label).Replace("\"", "'").Replace("\r", "").Replace("\n", " ").Trim();
            string desc = item.Description is { } d ? "<br/>" + d.Replace("\"", "'").Replace("\r", "").Replace("\n", " ").Trim() : "";
            return $"[\"{label}{desc}\"]";
        }
        List<ListItem> items = n.Items ?? new List<ListItem>();
        if (items.Count == 0) return $"{id}[{title}]\n";
        StringBuilder sb = new StringBuilder($"subgraph {id}[{title}]\n");
        int k = 0;
        if (n.Type != NodeTypes.Ipo)
        {
            sb.Append("    direction TB\n");
            foreach (ListItem item in items)
            {
                string rid = $"{id}_{++k}";
                rowIds[(n.Id, item.Id)] = rid;
                sb.Append($"    {rid}{Row(item)}\n");
            }
        }
        else
        {
            sb.Append(n.Stacked ? "    direction TB\n" : "    direction LR\n");
            foreach (string section in IpoSections.All)
            {
                List<ListItem> rows = PanelGeometry.InSection(n, section);
                if (rows.Count == 0) continue;
                string sectionTitle = PanelGeometry.SectionTitle(n, section).Replace("\"", "'").Replace("\n", " ").Trim();
                sb.Append($"    subgraph {id}_{section}[\"{sectionTitle}\"]\n");
                foreach (ListItem item in rows)
                {
                    string rid = $"{id}_{++k}";
                    rowIds[(n.Id, item.Id)] = rid;
                    sb.Append($"        {rid}{Row(item)}\n");
                }
                sb.Append("    end\n");
            }
        }
        return sb.Append("end\n").ToString();
    }

    /// <summary>Mermaid flowchart text, for interoperability with Mermaid-based tools.</summary>
    public static string ToMermaid(Diagram d)
    {
        static string Q(string s) => "\"" + (s ?? "").Replace("\"", "'").Replace("\r", "").Replace("\n", " ").Trim() + "\"";
        StringBuilder sb = new StringBuilder("flowchart TD\n");
        Dictionary<string, string> ids = new Dictionary<string, string>();
        Dictionary<(string Node, string Item), string> rowIds = new Dictionary<(string Node, string Item), string>();
        Dictionary<string, string> panelBlocks = new Dictionary<string, string>();
        StringBuilder styles = new StringBuilder();
        int i = 0;
        foreach (Node n in d.Nodes)
        {
            string id = $"N{++i}";
            ids[n.Id] = id;
            string text = Geometry.ShownStep(d, n) is int step ? $"{step}. {Geometry.LabelText(n)}" : Geometry.LabelText(n);
            // Mermaid can't draw text outside a shape, so the description goes on its own line inside the label.
            string desc = n.Description is { } dsc ? "<br/>" + dsc.Replace("\"", "'").Replace("\r", "").Trim().Replace("\n", " ") : "";
            if (n.Kind.IsPanel)
            {
                // A list / IPO box is a subgraph with a node per row, so connectors can still end on a row.
                panelBlocks[n.Id] = PanelMermaid(n, id, Q(text)[..^1] + desc + "\"", rowIds);
                NodeStyle pst = NodeStyle.For(n);
                styles.Append($"    style {id} fill:{pst.Fill},stroke:{pst.Stroke},color:{pst.Text}\n");
                if (n.GroupId is null || d.Groups.All(g => g.Id != n.GroupId)) sb.Append(Indent(panelBlocks[n.Id], 1));
                continue;
            }
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
                sb.Append($"    {id}{open}{Q($"{sym} {text}")[..^1]}{desc}\"{close}\n");
                NodeStyle ost = NodeStyle.For(n);
                styles.Append($"    style {id} fill:{ost.Fill},stroke:{ost.Stroke},color:{(n.Filled ? ost.IconColor : "#1f2933")}\n");
                continue;
            }
            string label = Q(text)[..^1] + desc + "\"";
            string shape = Geometry.OutlineOf(n).Mermaid(label);
            sb.Append($"    {id}{shape}\n");
            // Mermaid has no halo: a major step keeps its solid fill and gets a heavy outline; a minor one goes light.
            if (n.Emphasis is not null)
            {
                NodeStyle est = NodeStyle.For(n);
                IEmphasis em = Emphases.Of(n);
                styles.Append($"    style {id} fill:{est.Fill},stroke:{est.Stroke},color:{est.Text},stroke-width:{F(em.HasHalo ? 4 : em.StrokeWidth)}px\n");
            }
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
        // Groups become subgraphs; a group inside another is a subgraph inside that one (ids follow file order).
        GroupTree tree = new GroupTree(d);
        int gi = 0;
        foreach (Group g in d.Groups) ids[g.Id] = $"G{++gi}";
        HashSet<string> written = new HashSet<string>();
        void WriteGroup(Group g, int depth)
        {
            if (!written.Add(g.Id)) return;
            string gid = ids[g.Id];
            string indent = new string(' ', 4 * (depth + 1));
            sb.Append($"{indent}subgraph {gid}[{Q(g.Title)}]\n");
            foreach (IGroupable m in tree.Children(g).Where(m => m is not Group))
                sb.Append(panelBlocks.TryGetValue(m.Id, out string? block) ? Indent(block, depth + 2) : $"{indent}    {ids[m.Id]}\n");
            // Carry the group's look across where Mermaid can show it (no fill, broken outline, line weight).
            List<string> look = new List<string>();
            if (!g.Filled) look.Add("fill:none");
            if (g.Border != LineLook.Solid) look.Add("stroke-dasharray:5 5");
            if (g.BorderWeight != LineLook.Regular) look.Add($"stroke-width:{F(GroupLook.Width(g.BorderWeight))}px");
            if (look.Count > 0) styles.Append($"    style {gid} {string.Join(",", look)}\n");
            foreach (Group inner in tree.Children(g).OfType<Group>()) WriteGroup(inner, depth + 1);
            sb.Append($"{indent}end\n");
        }
        foreach (Group g in d.Groups.Where(g => tree.Parent(g) is null)) WriteGroup(g, 0);
        foreach (Group g in d.Groups) WriteGroup(g, 0);   // only groups caught in a parent loop (hand-edited file)
        foreach (Edge e in d.Edges)
        {
            if (!ids.TryGetValue(e.From, out string? a) || !ids.TryGetValue(e.To, out string? b)) continue;
            if (e.FromItem is { } fromRow && rowIds.TryGetValue((e.From, fromRow), out string? ra)) a = ra;
            if (e.ToItem is { } toRow && rowIds.TryGetValue((e.To, toRow), out string? rb)) b = rb;
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
