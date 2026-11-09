using BeDiagram.Models.Kinds;
using BeDiagram.Models.Shapes;

namespace BeDiagram.Models.Catalogue;

/// <summary>The node types in palette order (1–9 quick-add keys follow it), with what each one starts with.</summary>
public static class NodeTypes
{
    // One shared instance per kind (they hold no state).
    static readonly INodeKind Classic = new ClassicKind();

    public static readonly IReadOnlyList<NodeTypeInfo> All = new List<NodeTypeInfo>
    {
        new(NodeType.Start,    "Start",         ShapeOutlines.Pill,          "#e7f5ec", "#2f8f5b", 140, 52) { Kind = Classic, Icon = "play", RoundIcon = "play" },
        new(NodeType.Process,  "Process",       ShapeOutlines.Rect,          "#eef3fb", "#3b6fb6", 160, 64) { Kind = Classic },
        new(NodeType.Decision, "Decision",      ShapeOutlines.Diamond,       "#fff6e5", "#c58a12", 170, 96)
            { Kind = Classic, RoundIcon = "git-branch", PropertyKeys = new[] { "condition" } },
        new(NodeType.Check,    "Rule check",    ShapeOutlines.Check,         "#f1ecfb", "#6a4bb5", 170, 64)
            { Kind = Classic, RoundIcon = "shield-check", PropertyKeys = new[] { "ruleId", "ifcEntity", "property", "operator", "value" } },
        new(NodeType.Action,   "Action",        ShapeOutlines.RoundedRect,   "#e8f6f8", "#1f8796", 160, 64)
            { Kind = Classic, RoundIcon = "zap", PropertyKeys = new[] { "owner" } },
        new(NodeType.Data,     "Data / IFC",    ShapeOutlines.Parallelogram, "#f4f4f2", "#6b6b66", 170, 64)
            { Kind = Classic, RoundIcon = "database", PropertyKeys = new[] { "source" } },
        new(NodeType.Status,   "Status",        ShapeOutlines.Hexagon,       "#fdeef0", "#b8475a", 150, 56)
            { Kind = Classic, RoundIcon = "activity", PropertyKeys = new[] { "bcfStatus" } },
        new(NodeType.End,      "End",           ShapeOutlines.Pill,          "#e7f5ec", "#2f8f5b", 140, 52) { Kind = Classic, Icon = "flag", RoundIcon = "flag" },
        new(NodeType.Circle,   "Circle",        ShapeOutlines.Circle,        "#eef3fb", "#3b6fb6", 84, 84) { Kind = new CircleKind(), Icon = "layers", RoundIcon = "layers" },
        // The outcome (pass, fail, ...) picks the drawn outline (OutcomeKind); this one is only a placeholder.
        new(NodeType.Outcome,  "Outcome",       ShapeOutlines.Rect,          "#e7f5ec", "#2f8f5b", 50, 50) { Kind = new OutcomeKind() },
        // After Outcome so the 1–9 shortcut keys stay on the classic shapes.
        new(NodeType.List,     "List",          ShapeOutlines.Panel,         "#f3f5f8", "#4f6478", 220, 200) { Kind = new ListKind() },
        new(NodeType.Ipo,      "Input / process / output", ShapeOutlines.Panel, "#fbf4ec", "#a8662a", 480, 150) { Kind = new IpoKind() },
    };

    /// <summary>Width and height of a circle-drawn shape (the same as a new Circle).</summary>
    public const double RoundSize = 84;

    static readonly Dictionary<NodeType, NodeTypeInfo> ByKey = All.ToDictionary(t => t.Key);

    public static NodeTypeInfo Get(NodeType type) => ByKey[type];
}
