using BeDiagram.Models;

namespace BeDiagram.Editing.Lanes;

/// <summary>Lanes side by side as columns, the flow running top → down (flow = y, cross = x).</summary>
public class VerticalLanes : ILaneLayout
{
    public const string KeyName = "vertical";

    public string Key => KeyName;
    public string Direction => "top → down";
    public string Name => "Lanes top → down";

    public (double Flow, double Cross) ToLane(double x, double y) => (y, x);

    public (double X, double Y) FromLane(double flow, double cross) => (cross, flow);

    public (double FlowStart, double FlowEnd, double CrossStart, double CrossEnd) Insets(Group g) =>
        (Geometry.GroupInsetTop(g) + Group.Pad, Geometry.GroupInsetBottom(g) + Group.Pad, Group.Pad, Group.Pad);
}
