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
        bool isOutcome = t.Key == NodeTypes.Outcome;
        return new Node
        {
            Type = t.Key,
            Outcome = isOutcome ? Outcomes.Get(outcome).Key : null,
            Filled = isOutcome,
            Label = t.Key == NodeTypes.Circle ? "Label" : isOutcome ? Outcomes.Get(outcome).Display : t.Display,
            Icon = DefaultIcon(t.Key),
            W = t.W,
            H = t.H,
            X = cx - t.W / 2,
            Y = cy - t.H / 2,
            Properties = DefaultProperties(t.Key),
        };
    }

    /// <summary>The icon a new node of <paramref name="type"/> starts with: start ▶ and end 🏁 so the two ends of a
    /// flow read as a pair (End is green like Start, so the flag is what tells them apart), a placeholder on circles.</summary>
    public static string? DefaultIcon(string type) => type switch
    {
        NodeTypes.Start => "play",
        NodeTypes.End => "flag",
        NodeTypes.Circle => "layers",
        _ => null,
    };

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

    /// <summary>Palette keys are a node type, or "outcome:&lt;variant&gt;" for an outcome.</summary>
    public static (string Type, string? Outcome) ParsePaletteKey(string key) =>
        key.StartsWith(NodeTypes.Outcome + ":") ? (NodeTypes.Outcome, key[(NodeTypes.Outcome.Length + 1)..]) : (key, null);
}
