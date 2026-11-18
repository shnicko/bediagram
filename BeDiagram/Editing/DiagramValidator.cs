using BeDiagram.Models;

namespace BeDiagram.Editing;

/// <summary>
/// Checks a diagram read from hand-edited JSON before it replaces the open one (the JSON editor's Apply). The JSON can
/// parse and still not make a drawable diagram: missing lists, items sharing an id, connectors to items that aren't
/// there, a group inside itself. Each problem comes back as one line naming the item, so it can be found in the text.
/// An empty list means the diagram is fine to use.
/// </summary>
public static class DiagramValidator
{
    public static List<string> Check(Diagram d)
    {
        List<string> errors = new List<string>();

        // Lists and settings the editor reads without checking for null (JSON can say null where the type can't).
        List<Node>? nodeList = d.Nodes;
        List<Edge>? edgeList = d.Edges;
        List<Group>? groupList = d.Groups;
        List<TextBox>? textList = d.TextBoxes;
        if (nodeList is null)
        {
            errors.Add("\"nodes\" must be a list (use [] for none).");
        }
        if (edgeList is null)
        {
            errors.Add("\"edges\" must be a list (use [] for none).");
        }
        if (groupList is null)
        {
            errors.Add("\"groups\" must be a list (use [] for none).");
        }
        if (textList is null)
        {
            errors.Add("\"textBoxes\" must be a list (use [] for none).");
        }
        if (d.Page is null)
        {
            errors.Add("\"page\" can't be null (leave it out for an open canvas).");
        }
        if (d.DefaultLook is null)
        {
            errors.Add("\"defaultLook\" can't be null (leave it out for the usual look).");
        }
        if (errors.Count > 0 || nodeList is null || edgeList is null || groupList is null || textList is null)
        {
            return errors;
        }

        // Shapes, text boxes and groups share one set of ids (connectors and groups point at them by id).
        Dictionary<string, IBox> boxes = new Dictionary<string, IBox>();
        Dictionary<string, Node> nodes = new Dictionary<string, Node>();
        Dictionary<string, Group> groups = new Dictionary<string, Group>();
        for (int i = 0; i < nodeList.Count; i++)
        {
            Node n = nodeList[i];
            if (n is null)
            {
                errors.Add($"nodes[{i}] is null.");
                continue;
            }
            if (AddBox(boxes, n, $"nodes[{i}]", errors))
            {
                nodes[n.Id] = n;
            }
            if (n.Properties is null)
            {
                errors.Add($"Shape {n.Id}: \"properties\" can't be null (use {{}} for none).");
            }
            if (n.Items is not null)
            {
                for (int r = 0; r < n.Items.Count; r++)
                {
                    if (n.Items[r] is null)
                    {
                        errors.Add($"Shape {n.Id}: items[{r}] is null.");
                    }
                }
            }
        }
        for (int i = 0; i < textList.Count; i++)
        {
            TextBox t = textList[i];
            if (t is null)
            {
                errors.Add($"textBoxes[{i}] is null.");
                continue;
            }
            AddBox(boxes, t, $"textBoxes[{i}]", errors);
        }
        for (int i = 0; i < groupList.Count; i++)
        {
            Group g = groupList[i];
            if (g is null)
            {
                errors.Add($"groups[{i}] is null.");
                continue;
            }
            if (AddBox(boxes, g, $"groups[{i}]", errors))
            {
                groups[g.Id] = g;
            }
        }

        // Group membership: the group must exist, and following groupId upwards must never come back round.
        foreach (IBox b in boxes.Values)
        {
            if (b is IGroupable item && item.GroupId is not null && !groups.ContainsKey(item.GroupId))
            {
                errors.Add($"{b.Id}: groupId \"{item.GroupId}\" isn't a group in this diagram.");
            }
        }
        foreach (Group g in groups.Values)
        {
            if (InsideItself(g, groups))
            {
                errors.Add($"Group {g.Id} ends up inside itself (follow its groupId upwards).");
            }
        }

        // Connectors: unique ids, both ends on items that exist, row ends on rows that exist.
        HashSet<string> edgeIds = new HashSet<string>();
        for (int i = 0; i < edgeList.Count; i++)
        {
            Edge e = edgeList[i];
            if (e is null)
            {
                errors.Add($"edges[{i}] is null.");
                continue;
            }
            string name = $"Connector {e.Id}";
            if (string.IsNullOrWhiteSpace(e.Id))
            {
                name = $"edges[{i}]";
                errors.Add($"{name} has no id.");
            }
            else if (!edgeIds.Add(e.Id))
            {
                errors.Add($"{name}: another connector has the same id.");
            }
            CheckEnd(e.From, e.FromItem, "from", name, boxes, nodes, errors);
            CheckEnd(e.To, e.ToItem, "to", name, boxes, nodes, errors);
        }
        return errors;
    }

    /// <summary>Adds a shape, text box or group to the id table, reporting a missing or repeated id and a size that
    /// can't be drawn. True when it went in under its id.</summary>
    static bool AddBox(Dictionary<string, IBox> boxes, IBox b, string where, List<string> errors)
    {
        if (b.W <= 0 || b.H <= 0)
        {
            string id = b.Id;
            if (string.IsNullOrWhiteSpace(id))
            {
                id = where;
            }
            errors.Add($"{id}: width and height must be more than 0 (w {b.W}, h {b.H}).");
        }
        if (string.IsNullOrWhiteSpace(b.Id))
        {
            errors.Add($"{where} has no id.");
            return false;
        }
        if (boxes.ContainsKey(b.Id))
        {
            errors.Add($"{where}: id \"{b.Id}\" is already used by another shape, text box or group.");
            return false;
        }
        boxes[b.Id] = b;
        return true;
    }

    /// <summary>Follows the group's groupId chain; true if it reaches the group again.</summary>
    static bool InsideItself(Group g, Dictionary<string, Group> groups)
    {
        HashSet<string> seen = new HashSet<string>();
        seen.Add(g.Id);
        string? parentId = g.GroupId;
        while (parentId is not null)
        {
            if (!seen.Add(parentId))
            {
                return parentId == g.Id;
            }
            Group? parent;
            if (!groups.TryGetValue(parentId, out parent))
            {
                return false;
            }
            parentId = parent.GroupId;
        }
        return false;
    }

    /// <summary>One end of a connector: the item it points at must exist, and a row end must name a row of that shape.</summary>
    static void CheckEnd(string? boxId, string? rowId, string side, string name,
        Dictionary<string, IBox> boxes, Dictionary<string, Node> nodes, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(boxId))
        {
            errors.Add($"{name}: \"{side}\" is empty.");
            return;
        }
        if (!boxes.ContainsKey(boxId))
        {
            errors.Add($"{name}: \"{side}\" is \"{boxId}\", which isn't a shape, text box or group in this diagram.");
            return;
        }
        if (rowId is null)
        {
            return;
        }
        Node? n;
        if (!nodes.TryGetValue(boxId, out n) || n.Items is null)
        {
            errors.Add($"{name}: \"{side}Item\" is set but {boxId} has no rows.");
            return;
        }
        foreach (ListItem row in n.Items)
        {
            if (row is not null && row.Id == rowId)
            {
                return;
            }
        }
        errors.Add($"{name}: \"{side}Item\" is \"{rowId}\", which isn't a row of {boxId}.");
    }
}
