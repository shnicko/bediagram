using BeDiagram.Models;

namespace BeDiagram.Editing.Lanes;

/// <summary>
/// Which way swim lanes run (Strategy pattern, used by <see cref="LaneArranger"/>). The arranger works in lane
/// coordinates: <c>flow</c> runs along a lane (the direction the process goes) and <c>cross</c> runs across the lanes.
/// A layout says how those map onto the canvas's x and y, and how much room a lane's title takes at each end.
/// </summary>
public interface ILaneLayout
{
    /// <summary>Key used by the commands and buttons ("horizontal", "vertical").</summary>
    string Key { get; }
    /// <summary>Which way the flow runs, for messages: "left → right" or "top → down".</summary>
    string Direction { get; }
    /// <summary>Button text, e.g. "Lanes left → right".</summary>
    string Name { get; }

    /// <summary>A canvas point or size as (flow, cross).</summary>
    (double Flow, double Cross) ToLane(double x, double y);

    /// <summary>A (flow, cross) point or size back on the canvas as (x, y).</summary>
    (double X, double Y) FromLane(double flow, double cross);

    /// <summary>Room inside lane <paramref name="g"/> before and after its items, along the flow and across it
    /// (padding plus the title band where it sits).</summary>
    (double FlowStart, double FlowEnd, double CrossStart, double CrossEnd) Insets(Group g);
}
