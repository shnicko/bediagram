using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models;

/// <summary>
/// Factory for new shapes: one place that knows what a fresh node of each type looks like — its size, label, icon,
/// fill and the default properties the audit tool reads (rule checks get ruleId / ifcEntity / property / operator / value).
/// </summary>
public static class NodeFactory
{
    /// <summary>
    /// A new node for a palette key (a node type, or "outcome:&lt;variant&gt;"), centred on (<paramref name="cx"/>,
    /// <paramref name="cy"/>). The caller snaps the centre and adds the node to the diagram.
    /// </summary>
    public static Node Create(string paletteKey, double cx, double cy)
    {
        (string type, string? outcome) = ParsePaletteKey(paletteKey);
        NodeTypeInfo t = NodeTypes.Get(type);
        bool round = IsRoundKey(paletteKey) && NodeTypes.CanBeRound(t.Key);
        (double w, double h) = round ? (NodeTypes.RoundSize, NodeTypes.RoundSize) : (t.W, t.H);
        bool isOutcome = t.Key == NodeTypes.Outcome;
        Node n = new Node
        {
            Type = t.Key,
            Outcome = isOutcome ? Outcomes.Get(outcome).Key : null,
            Filled = isOutcome,
            Label = t.Key == NodeTypes.Circle ? "Label" : t.Key == NodeTypes.Ipo ? "Process" : isOutcome ? Outcomes.Get(outcome).Display : t.Display,
            Round = round,
            Icon = round ? RoundIcon(t.Key) : DefaultIcon(t.Key),
            W = w,
            H = h,
            X = cx - w / 2,
            Y = cy - h / 2,
            Properties = DefaultProperties(t.Key),
            Items = NodeTypes.IsPanel(t.Key) ? DefaultItems(t.Key) : null,
        };
        if (n.Items is not null)
        {
            // Rows have a fixed height, so the node is as tall as its rows need.
            n.H = PanelGeometry.NeededHeight(n);
            n.Y = cy - n.H / 2;
        }
        return n;
    }

    /// <summary>The rows a new list (three items) or IPO box (one per section) starts with.</summary>
    public static List<ListItem> DefaultItems(string type) => type == NodeTypes.Ipo
        ? new List<ListItem>
        {
            new() { Label = "Inputs", Section = IpoSections.Input },
            new() { Label = "Work done", Section = IpoSections.Process },
            new() { Label = "Results", Section = IpoSections.Output },
        }
        : new List<ListItem> { new() { Label = "Item 1" }, new() { Label = "Item 2" }, new() { Label = "Item 3" } };

    /// <summary>The icon a new node of <paramref name="type"/> starts with: start ▶ and end 🏁 so the two ends of a
    /// flow read as a pair (End is green like Start, so the flag is what tells them apart), a placeholder on circles.</summary>
    public static string? DefaultIcon(string type) => type switch
    {
        NodeTypes.Start => "play",
        NodeTypes.End => "flag",
        NodeTypes.Circle => "layers",
        _ => null,
    };

    /// <summary>The icon a shape drawn as a circle (<see cref="Node.Round"/>) starts with, so every circle of a type looks
    /// the same: start ▶, process gear, decision branch, rule check shield, action zap, data database, status activity, end 🏁.</summary>
    public static string RoundIcon(string type) => type switch
    {
        NodeTypes.Start => "play",
        NodeTypes.Decision => "git-branch",
        NodeTypes.Check => "shield-check",
        NodeTypes.Action => "zap",
        NodeTypes.Data => "database",
        NodeTypes.Status => "activity",
        NodeTypes.End => "flag",
        NodeTypes.Circle => "layers",
        _ => "settings",
    };

    /// <summary>Palette key prefix for a classic shape drawn as a circle, e.g. "round:decision".</summary>
    public const string RoundPrefix = "round:";

    public static bool IsRoundKey(string key) => key.StartsWith(RoundPrefix);

    /// <summary>Width and height a new node from this palette key gets (before rows are counted for lists / IPO boxes).</summary>
    public static (double W, double H) SizeFor(string paletteKey)
    {
        NodeTypeInfo t = NodeTypes.Get(ParsePaletteKey(paletteKey).Type);
        return IsRoundKey(paletteKey) && NodeTypes.CanBeRound(t.Key) ? (NodeTypes.RoundSize, NodeTypes.RoundSize) : (t.W, t.H);
    }

    /// <summary>The properties a new node of <paramref name="type"/> starts with (empty values).</summary>
    public static Dictionary<string, string> DefaultProperties(string type) => type switch
    {
        NodeTypes.Check => new() { ["ruleId"] = "", ["ifcEntity"] = "", ["property"] = "", ["operator"] = "", ["value"] = "" },
        NodeTypes.Decision => new() { ["condition"] = "" },
        NodeTypes.Data => new() { ["source"] = "" },
        NodeTypes.Status => new() { ["bcfStatus"] = "" },
        NodeTypes.Action => new() { ["owner"] = "" },
        _ => new(),
    };

    /// <summary>Palette keys are a node type, "outcome:&lt;variant&gt;" for an outcome, or "round:&lt;type&gt;" for a shape drawn as a circle.</summary>
    public static (string Type, string? Outcome) ParsePaletteKey(string key) =>
        key.StartsWith(NodeTypes.Outcome + ":") ? (NodeTypes.Outcome, key[(NodeTypes.Outcome.Length + 1)..])
        : IsRoundKey(key) ? (key[RoundPrefix.Length..], null)
        : (key, null);
}
