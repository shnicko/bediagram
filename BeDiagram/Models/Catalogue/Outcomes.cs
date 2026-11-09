using BeDiagram.Models.Shapes;

namespace BeDiagram.Models.Catalogue;

public static class Outcomes
{
    public static readonly IReadOnlyList<OutcomeInfo> All = new List<OutcomeInfo>
    {
        new(OutcomeType.Pass,        "Pass",        ShapeOutlines.Seal,          "outcome-tick",    "green", "Accepted / approved")
            { MermaidSymbol = "✓", MermaidOpen = "((", MermaidClose = "))" },
        new(OutcomeType.Fail,        "Fail",        ShapeOutlines.Octagon,       "outcome-cross",   "red",   "Rejected — stops here")
            { MermaidSymbol = "✕", MermaidOpen = "{{", MermaidClose = "}}" },
        new(OutcomeType.Rework,      "Rework",      ShapeOutlines.Circle,        "outcome-rework",  "pink",  "Returned to the author for rework")
            { MermaidSymbol = "↺", MermaidOpen = "((", MermaidClose = "))" },
        new(OutcomeType.Conditional, "Conditional", ShapeOutlines.Triangle,      "outcome-exclaim", "amber", "Pass with comments / approved subject to changes")
            { MermaidSymbol = "⚠", MermaidOpen = "[/", MermaidClose = "\\]" },
        new(OutcomeType.Hold,        "Hold",        ShapeOutlines.RoundedSquare, "outcome-clock",   "slate", "On hold / awaiting review")
            { MermaidSymbol = "⏸", MermaidOpen = "(", MermaidClose = ")" },
    };

    /// <summary>The outcome's details; pass when there's none.</summary>
    public static OutcomeInfo Get(OutcomeType? outcome) => All.FirstOrDefault(o => o.Key == outcome) ?? All[0];
}
