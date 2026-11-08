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

    public static readonly IReadOnlyList<NodeTypeInfo> All = new List<NodeTypeInfo>
    {
        new(Start,    "Start",         "pill",          "#e7f5ec", "#2f8f5b", 140, 52),
        new(Process,  "Process",       "rect",          "#eef3fb", "#3b6fb6", 160, 64),
        new(Decision, "Decision",      "diamond",       "#fff6e5", "#c58a12", 170, 96),
        new(Check,    "Rule check",    "check",         "#f1ecfb", "#6a4bb5", 170, 64),
        new(Action,   "Action",        "rect-rounded",  "#e8f6f8", "#1f8796", 160, 64),
        new(Data,     "Data / IFC",    "parallelogram", "#f4f4f2", "#6b6b66", 170, 64),
        new(Status,   "Status",        "hexagon",       "#fdeef0", "#b8475a", 150, 56),
        new(End,      "End",           "pill",          "#e7f5ec", "#2f8f5b", 140, 52),
        new(Circle,   "Circle",        "circle",        "#eef3fb", "#3b6fb6", 84, 84),
        new(Outcome,  "Outcome",       "outcome",       "#e7f5ec", "#2f8f5b", 50, 50),
        // After Outcome so the 1–9 shortcut keys stay on the classic shapes.
        new(List,     "List",          "panel",         "#f3f5f8", "#4f6478", 220, 200),
        new(Ipo,      "Input / process / output", "panel", "#fbf4ec", "#a8662a", 480, 150),
    };

    /// <summary>List and IPO nodes: a titled box holding rows (<see cref="Node.Items"/>), laid out by <see cref="PanelGeometry"/>.</summary>
    public static bool IsPanel(string type) => type is List or Ipo;

    /// <summary>Types that can also be drawn as a circle (<see cref="Node.Round"/>): the eight classic shapes.
    /// Circles already are one; outcomes, lists and IPO boxes have shapes of their own.</summary>
    public static bool CanBeRound(string type) => type is not (Circle or Outcome or List or Ipo);

    /// <summary>Width and height of a circle-drawn shape (the same as a new Circle).</summary>
    public const double RoundSize = 84;

    public static NodeTypeInfo Get(string key) => All.FirstOrDefault(t => t.Key == key) ?? All[1];
}
