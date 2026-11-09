using System.Globalization;
using System.Text;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Kinds;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;

namespace BeDiagram.Services.Export;

/// <summary>
/// Mermaid text: the structure of the diagram (shapes, groups as subgraphs, connectors), for Mermaid-based tools.
/// Mermaid has no page or window, so there is no area to choose.
/// </summary>
public class MermaidExportFormat : IExportFormat
{
    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    public string Name => "Mermaid text (.mmd)";
    public string Extension => ".mmd";
    public bool UsesArea => false;

    public Task<ExportFile> RenderAsync(Diagram d, ExportView? view, ExportOptions options) =>
        Task.FromResult(new ExportFile(Encoding.UTF8.GetBytes(Write(d))));

    /// <summary>Mermaid link syntax: dotted for any broken pattern, thick for heavy solid lines.</summary>
    string MermaidLink(Edge e)
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
    string Indent(string block, int depth)
    {
        string pad = new string(' ', 4 * depth);
        return string.Concat(block.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => pad + l + "\n"));
    }

    /// <summary>
    /// A list / IPO node as Mermaid (not indented): a subgraph titled with <paramref name="title"/> (already quoted)
    /// holding a node per row (ids "N3_1", "N3_2", … recorded in <paramref name="rowIds"/>); an IPO box nests a
    /// subgraph per section that has rows. A panel with no rows at all is a plain node.
    /// </summary>
    string PanelMermaid(Node n, string id, string title, Dictionary<(string Node, string Item), string> rowIds)
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
        if (n.Kind is not IpoKind)
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
                List<ListItem> rows = n.RowsIn(section);
                if (rows.Count == 0) continue;
                string sectionTitle = n.SectionTitle(section).Replace("\"", "'").Replace("\n", " ").Trim();
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
    public string Write(Diagram d)
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
            if (n.Kind.HasOutcome)
            {
                // Mermaid has no seal/octagon: a symbol in the label plus the closest classic shape and a solid fill.
                OutcomeInfo o = Outcomes.Get(n.Outcome);
                sb.Append($"    {id}{o.MermaidOpen}{Q($"{o.MermaidSymbol} {text}")[..^1]}{desc}\"{o.MermaidClose}\n");
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
