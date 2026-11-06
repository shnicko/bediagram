using BeDiagram.Models;

namespace BeDiagram.Editing.Lanes;

/// <summary>Lanes stacked as rows, the flow running left → right (flow = x, cross = y).</summary>
public class HorizontalLanes : ILaneLayout
{
    public const string KeyName = "horizontal";

    public string Key => KeyName;
    public string Name => "Lanes left → right";

    public (double Flow, double Cross) ToLane(double x, double y) => (x, y);

    public (double X, double Y) FromLane(double flow, double cross) => (flow, cross);

    public (double FlowStart, double FlowEnd, double CrossStart, double CrossEnd) Insets(Group g) =>
        (Group.Pad, Group.Pad, Geometry.GroupInsetTop(g) + Group.Pad, Geometry.GroupInsetBottom(g) + Group.Pad);
}
