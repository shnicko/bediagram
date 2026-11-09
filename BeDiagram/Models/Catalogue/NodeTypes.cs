using BeDiagram.Models.Kinds;

namespace BeDiagram.Models.Catalogue;

public static class NodeTypes
{
    public const string Start = "start";
    public const string End = "end";
    public const string Process = "process";
    public const string Decision = "decision";
    public const string Check = "check";
    public const string Action = "action";
    public const string Data = "data";
    public const string Status = "status";
    public const string Circle = "circle";
    public const string Outcome = "outcome";
    public const string List = "list";
    public const string Ipo = "ipo";

    // One shared instance per kind (they hold no state).
    static readonly INodeKind Classic = new ClassicKind();

    public static readonly IReadOnlyList<NodeTypeInfo> All = new List<NodeTypeInfo>
    {
        new(Start,    "Start",         "pill",          "#e7f5ec", "#2f8f5b", 140, 52) { Kind = Classic, Icon = "play", RoundIcon = "play" },
        new(Process,  "Process",       "rect",          "#eef3fb", "#3b6fb6", 160, 64) { Kind = Classic },
        new(Decision, "Decision",      "diamond",       "#fff6e5", "#c58a12", 170, 96)
            { Kind = Classic, RoundIcon = "git-branch", PropertyKeys = new[] { "condition" } },
        new(Check,    "Rule check",    "check",         "#f1ecfb", "#6a4bb5", 170, 64)
            { Kind = Classic, RoundIcon = "shield-check", PropertyKeys = new[] { "ruleId", "ifcEntity", "property", "operator", "value" } },
        new(Action,   "Action",        "rect-rounded",  "#e8f6f8", "#1f8796", 160, 64)
            { Kind = Classic, RoundIcon = "zap", PropertyKeys = new[] { "owner" } },
        new(Data,     "Data / IFC",    "parallelogram", "#f4f4f2", "#6b6b66", 170, 64)
            { Kind = Classic, RoundIcon = "database", PropertyKeys = new[] { "source" } },
        new(Status,   "Status",        "hexagon",       "#fdeef0", "#b8475a", 150, 56)
            { Kind = Classic, RoundIcon = "activity", PropertyKeys = new[] { "bcfStatus" } },
        new(End,      "End",           "pill",          "#e7f5ec", "#2f8f5b", 140, 52) { Kind = Classic, Icon = "flag", RoundIcon = "flag" },
        new(Circle,   "Circle",        "circle",        "#eef3fb", "#3b6fb6", 84, 84) { Kind = new CircleKind(), Icon = "layers", RoundIcon = "layers" },
        new(Outcome,  "Outcome",       "outcome",       "#e7f5ec", "#2f8f5b", 50, 50) { Kind = new OutcomeKind() },
        // After Outcome so the 1–9 shortcut keys stay on the classic shapes.
        new(List,     "List",          "panel",         "#f3f5f8", "#4f6478", 220, 200) { Kind = new ListKind() },
        new(Ipo,      "Input / process / output", "panel", "#fbf4ec", "#a8662a", 480, 150) { Kind = new IpoKind() },
    };

    /// <summary>Width and height of a circle-drawn shape (the same as a new Circle).</summary>
    public const double RoundSize = 84;

    // Looked up on every draw (Node.Kind), so by dictionary rather than a scan of All.
    static readonly Dictionary<string, NodeTypeInfo> ByKey = All.ToDictionary(t => t.Key);

    /// <summary>The type for <paramref name="key"/>; Process for an unknown key.</summary>
    public static NodeTypeInfo Get(string key) => ByKey.TryGetValue(key, out NodeTypeInfo? t) ? t : All[1];
}
