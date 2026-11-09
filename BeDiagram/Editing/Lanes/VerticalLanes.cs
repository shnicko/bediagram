using BeDiagram.Models;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Editing.Lanes;

/// <summary>Lanes side by side as columns, the flow running top → down (flow = y, cross = x).</summary>
public class VerticalLanes : ILaneLayout
{
    public const string KeyName = "vertical";

    public string Key => KeyName;
    public string Direction => "top → down";
    public FlowDirection Flow => FlowDirection.Down;
    public string Name => "Lanes top → down";

    public (double Flow, double Cross) ToLane(double x, double y) => (y, x);

    public (double X, double Y) FromLane(double flow, double cross) => (cross, flow);

    public (double FlowStart, double FlowEnd, double CrossStart, double CrossEnd) Insets(Group g) =>
        (g.InsetTop() + Group.Pad, g.InsetBottom() + Group.Pad, Group.Pad, Group.Pad);
}
