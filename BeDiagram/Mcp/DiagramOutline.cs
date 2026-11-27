using System.Globalization;
using System.Text;
using BeDiagram.Models;
using BeDiagram.Models.Serialization;

namespace BeDiagram.Mcp;

/// <summary>
/// A diagram as a short text outline for the model: settings, then every shape, row, group, text box and connector
/// with its id. Much smaller than the file JSON (no colours, fonts or routes), so it is the cheap way for Claude to see
/// what is there before editing.
/// </summary>
public static class DiagramOutline
{
    public static string Of(Diagram d)
    {
        StringBuilder text = new StringBuilder();
        text.AppendLine("Diagram \"" + d.Name + "\"");
        text.AppendLine("Flow: " + EnumNames.Of(d.Flow) + ". Page: " + (d.Page.Enabled ? d.Page.Describe() : "none (open canvas)") + ".");
        if (!string.IsNullOrWhiteSpace(d.Title))
        {
            text.AppendLine("Title: " + d.Title);
        }
        if (!string.IsNullOrWhiteSpace(d.Description))
        {
            text.AppendLine("Description: " + OneLine(d.Description));
        }

        text.AppendLine();
        text.AppendLine("Shapes (" + d.Nodes.Count + "): id, type, step, label, position and size (x, y = top-left)");
        foreach (Node n in d.Nodes)
        {
            text.AppendLine("  " + Shape(n));
            if (n.Description is not null)
            {
                text.AppendLine("      description: " + OneLine(n.Description));
            }
            foreach (ListItem row in n.Rows)
            {
                text.AppendLine("      row " + row.Id + ": " + OneLine(row.Label));
            }
        }

        if (d.Groups.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Groups (" + d.Groups.Count + "): top-level groups are swim lanes");
            foreach (Group g in d.Groups)
            {
                text.AppendLine("  " + g.Id + "  \"" + OneLine(g.Title) + "\"" + InGroup(g.GroupId));
            }
        }

        if (d.TextBoxes.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Text boxes (" + d.TextBoxes.Count + ")");
            foreach (TextBox t in d.TextBoxes)
            {
                text.AppendLine("  " + t.Id + (t.IsComment ? "  comment" : "  text") + "  \"" + OneLine(t.Text) + "\"" + InGroup(t.GroupId));
            }
        }

        text.AppendLine();
        text.AppendLine("Connectors (" + d.Edges.Count + ")");
        foreach (Edge e in d.Edges)
        {
            text.AppendLine("  " + Connector(e));
        }
        return text.ToString();
    }

    static string Shape(Node n)
    {
        string type = EnumNames.Of(n.Type);
        if (n.Outcome is not null)
        {
            type = type + " (" + EnumNames.Of(n.Outcome.Value) + ")";
        }
        if (n.Round)
        {
            type = type + " (round)";
        }
        string step = "";
        if (n.Step is not null)
        {
            step = "  step " + n.Step.Value.ToString(CultureInfo.InvariantCulture);
        }
        return n.Id + "  " + type + step + "  \"" + OneLine(n.Label) + "\"  at " + Number(n.X) + "," + Number(n.Y)
            + "  " + Number(n.W) + "x" + Number(n.H) + InGroup(n.GroupId);
    }

    static string Connector(Edge e)
    {
        string from = e.From;
        if (e.FromItem is not null)
        {
            from = from + " row " + e.FromItem;
        }
        string to = e.To;
        if (e.ToItem is not null)
        {
            to = to + " row " + e.ToItem;
        }
        string label = "";
        if (e.Label.Length > 0)
        {
            label = "  \"" + OneLine(e.Label) + "\"";
        }
        return e.Id + "  " + from + " -> " + to + label;
    }

    static string InGroup(string? groupId)
    {
        if (groupId is null)
        {
            return "";
        }
        return "  in " + groupId;
    }

    static string Number(double v)
    {
        return Math.Round(v).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>Text on one line: line breaks shown as " / ".</summary>
    static string OneLine(string? s)
    {
        if (s is null)
        {
            return "";
        }
        return s.Replace("\r\n", " / ").Replace("\n", " / ");
    }
}
