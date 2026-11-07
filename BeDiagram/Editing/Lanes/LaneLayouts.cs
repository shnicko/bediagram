namespace BeDiagram.Editing.Lanes;

/// <summary>The lane layouts, by key (like <c>AngleLocks</c> and <c>Emphases</c>).</summary>
public static class LaneLayouts
{
    public static readonly ILaneLayout Horizontal = new HorizontalLanes();
    public static readonly ILaneLayout Vertical = new VerticalLanes();

    public static IReadOnlyList<ILaneLayout> All { get; } = new List<ILaneLayout> { Horizontal, Vertical };

    public static ILaneLayout Get(string key) => All.FirstOrDefault(l => l.Key == key) ?? Horizontal;

    /// <summary>The layout whose flow runs the way <paramref name="flow"/> says (a <c>FlowDirections</c> key); top → down
    /// when unknown. Quick-add uses it to work in (flow, cross) coordinates.</summary>
    public static ILaneLayout ForFlow(string? flow) => All.FirstOrDefault(l => l.Flow == flow) ?? Vertical;
}
