using BeDiagram.Models.Catalogue;

namespace BeDiagram.Editing.Lanes;

/// <summary>The lane layouts (like <c>AngleLocks</c> and <c>Emphases</c>), and the one for a flow direction.</summary>
public static class LaneLayouts
{
    public static readonly ILaneLayout Horizontal = new HorizontalLanes();
    public static readonly ILaneLayout Vertical = new VerticalLanes();

    public static IReadOnlyList<ILaneLayout> All { get; } = new List<ILaneLayout> { Horizontal, Vertical };

    /// <summary>The layout whose flow runs the way <paramref name="flow"/> says. Quick-add uses it to work in
    /// (flow, cross) coordinates.</summary>
    public static ILaneLayout ForFlow(FlowDirection flow) => All.First(l => l.Flow == flow);
}
