using BeDiagram.Editing.Routing;
using BeDiagram.Models;
using BeDiagram.Models.Kinds;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Text;

namespace BeDiagram.Cli;

/// <summary>
/// Looks for problems in a diagram that would show on the page or break it for other tools: repeated or missing ids,
/// connectors and row ends that point nowhere, connectors through shapes, items off the page, shapes on top of each
/// other and list rows cut short with "…". Changes nothing.
/// </summary>
public class DiagramChecker
{
    readonly Diagram d;
    readonly Dictionary<string, IBox> boxes = new Dictionary<string, IBox>();
    readonly List<string> problems = new List<string>();

    public DiagramChecker(Diagram diagram)
    {
        d = diagram;
    }

    /// <summary>Runs every check and returns the problems found, one line each (empty when it's clean).</summary>
    public List<string> Check()
    {
        problems.Clear();
        CheckIds();
        CheckGroups();
        CheckConnectors();
        CheckRoutes();
        CheckPage();
        CheckOverlaps();
        CheckRows();
        return new List<string>(problems);
    }

    // ───────────── Ids ─────────────

    /// <summary>Every shape, text box, group and connector needs its own id. Also fills the box lookup.</summary>
    void CheckIds()
    {
        boxes.Clear();
        HashSet<string> seen = new HashSet<string>();
        foreach (Node n in d.Nodes)
        {
            Remember(seen, n.Id, "Shape " + Describe(n));
            boxes[n.Id] = n;
        }
        foreach (TextBox t in d.TextBoxes)
        {
            Remember(seen, t.Id, "Text box " + Describe(t));
            boxes[t.Id] = t;
        }
        foreach (Group g in d.Groups)
        {
            Remember(seen, g.Id, "Group " + Describe(g));
            boxes[g.Id] = g;
        }
        foreach (Edge e in d.Edges)
        {
            Remember(seen, e.Id, "Connector " + e.Id);
        }
    }

    void Remember(HashSet<string> seen, string id, string what)
    {
        if (!seen.Add(id))
        {
            problems.Add(what + ": id " + id + " is used more than once");
        }
    }

    // ───────────── Groups ─────────────

    /// <summary>A GroupId must name a group that exists.</summary>
    void CheckGroups()
    {
        HashSet<string> groups = new HashSet<string>();
        foreach (Group g in d.Groups)
        {
            groups.Add(g.Id);
        }
        foreach (Node n in d.Nodes)
        {
            if (n.GroupId is not null && !groups.Contains(n.GroupId))
            {
                problems.Add("Shape " + Describe(n) + ": its group " + n.GroupId + " doesn't exist");
            }
        }
        foreach (TextBox t in d.TextBoxes)
        {
            if (t.GroupId is not null && !groups.Contains(t.GroupId))
            {
                problems.Add("Text box " + Describe(t) + ": its group " + t.GroupId + " doesn't exist");
            }
        }
        foreach (Group g in d.Groups)
        {
            if (g.GroupId is not null && !groups.Contains(g.GroupId))
            {
                problems.Add("Group " + Describe(g) + ": its parent group " + g.GroupId + " doesn't exist");
            }
        }
    }

    // ───────────── Connectors ─────────────

    /// <summary>Both ends of a connector must exist, and a row end must name a row of that shape.</summary>
    void CheckConnectors()
    {
        foreach (Edge e in d.Edges)
        {
            string name = DescribeEdge(e);
            IBox? a = Find(e.From);
            IBox? b = Find(e.To);
            if (a is null)
            {
                problems.Add(name + ": start " + e.From + " doesn't exist");
            }
            if (b is null)
            {
                problems.Add(name + ": end " + e.To + " doesn't exist");
            }
            CheckRowEnd(name, a, e.FromItem, "start");
            CheckRowEnd(name, b, e.ToItem, "end");
        }
    }

    void CheckRowEnd(string name, IBox? box, string? itemId, string which)
    {
        if (itemId is null || box is null)
        {
            return;
        }
        if (box is Node n)
        {
            foreach (ListItem item in n.Rows)
            {
                if (item.Id == itemId)
                {
                    return;
                }
            }
        }
        problems.Add(name + ": " + which + " row " + itemId + " isn't a row of " + box.Id);
    }

    /// <summary>Connectors that run through a shape, a text box or the text under a shape (what Tidy connectors fixes).</summary>
    void CheckRoutes()
    {
        TidyPlan plan = new ConnectorTidier(d, Find).Plan(d.Edges);
        if (plan.Crossing > 0)
        {
            problems.Add(plan.Crossing + " connector(s) pass through a shape or its text — run tidy ("
                         + plan.Cleared + " can be cleared)");
        }
        else if (plan.Routes.Count > 0)
        {
            problems.Add(plan.Routes.Count + " connector(s) could be routed better — run tidy");
        }
    }

    // ───────────── Page ─────────────

    /// <summary>On a fixed page, every shape, text box and group should sit inside the drawing area.</summary>
    void CheckPage()
    {
        if (!d.Page.Enabled)
        {
            return;
        }
        Rect area = PageHeader.DrawingArea(d);
        foreach (Node n in d.Nodes)
        {
            CheckInside(area, n, "Shape " + Describe(n));
        }
        foreach (TextBox t in d.TextBoxes)
        {
            CheckInside(area, t, "Text box " + Describe(t));
        }
        foreach (Group g in d.Groups)
        {
            CheckInside(area, g, "Group " + Describe(g));
        }
    }

    void CheckInside(Rect area, IBox b, string what)
    {
        // Half a pixel of slack, so a box exactly on the edge isn't reported.
        const double Slack = 0.5;
        bool inside = b.X >= area.X - Slack && b.Y >= area.Y - Slack
                      && b.X + b.W <= area.X + area.W + Slack && b.Y + b.H <= area.Y + area.H + Slack;
        if (!inside)
        {
            problems.Add(what + " is outside the page's drawing area (margins and header)");
        }
    }

    // ───────────── Overlaps ─────────────

    /// <summary>Two shapes drawn on top of each other (more than a couple of pixels).</summary>
    void CheckOverlaps()
    {
        const double Touch = 2;
        for (int i = 0; i < d.Nodes.Count; i++)
        {
            Node a = d.Nodes[i];
            for (int j = i + 1; j < d.Nodes.Count; j++)
            {
                Node b = d.Nodes[j];
                bool overlap = a.X + Touch < b.X + b.W && b.X + Touch < a.X + a.W
                               && a.Y + Touch < b.Y + b.H && b.Y + Touch < a.Y + a.H;
                if (overlap)
                {
                    problems.Add("Shapes " + Describe(a) + " and " + Describe(b) + " overlap");
                }
            }
        }
    }

    // ───────────── Rows ─────────────

    /// <summary>List and IPO rows whose label or description is cut short with "…" because the shape is too narrow.</summary>
    void CheckRows()
    {
        foreach (Node n in d.Nodes)
        {
            if (n.Kind is PanelKind panel)
            {
                foreach (PanelRow row in panel.LayoutOf(n).Rows(n))
                {
                    bool titleCut = row.Title.EndsWith("…") && !row.Item.Label.TrimEnd().EndsWith("…");
                    bool descCut = row.Desc is not null && row.Desc.EndsWith("…")
                                   && row.Item.Description is not null && !row.Item.Description.TrimEnd().EndsWith("…");
                    if (titleCut || descCut)
                    {
                        problems.Add("Shape " + Describe(n) + ": row \"" + row.Item.Label + "\" is cut short — widen the shape or shorten the text");
                    }
                }
            }
        }
    }

    // ───────────── Helpers ─────────────

    IBox? Find(string? id)
    {
        IBox? b;
        if (id is not null && boxes.TryGetValue(id, out b))
        {
            return b;
        }
        return null;
    }

    static string Describe(Node n)
    {
        return Quote(n.Label) + " (" + n.Id + ")";
    }

    static string Describe(TextBox t)
    {
        return Quote(t.Text) + " (" + t.Id + ")";
    }

    static string Describe(Group g)
    {
        return Quote(g.Title) + " (" + g.Id + ")";
    }

    string DescribeEdge(Edge e)
    {
        if (string.IsNullOrWhiteSpace(e.Label))
        {
            return "Connector " + e.Id;
        }
        return "Connector " + Quote(e.Label) + " (" + e.Id + ")";
    }

    /// <summary>The first line of a text, cut to 40 characters, in quotes.</summary>
    static string Quote(string text)
    {
        string line = text.Replace("\r", "").Split('\n')[0].Trim();
        if (line.Length > 40)
        {
            line = line.Substring(0, 39) + "…";
        }
        return "\"" + line + "\"";
    }
}
