namespace BeDiagram.Models.Catalogue;

public static class Outcomes
{
    public const string Pass = "pass", Fail = "fail", Rework = "rework", Conditional = "conditional", Hold = "hold";

    public static readonly IReadOnlyList<OutcomeInfo> All = new List<OutcomeInfo>
    {
        new(Pass,        "Pass",        "seal",           "outcome-tick",    "green", "Accepted / approved"),
        new(Fail,        "Fail",        "octagon",        "outcome-cross",   "red",   "Rejected — stops here"),
        new(Rework,      "Rework",      "circle",         "outcome-rework",  "pink",  "Returned to the author for rework"),
        new(Conditional, "Conditional", "triangle",       "outcome-exclaim", "amber", "Pass with comments / approved subject to changes"),
        new(Hold,        "Hold",        "square-rounded", "outcome-clock",   "slate", "On hold / awaiting review"),
    };

    public static OutcomeInfo Get(string? key) => All.FirstOrDefault(o => o.Key == key) ?? All[0];
}
