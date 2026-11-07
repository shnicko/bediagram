namespace BeDiagram.Models.Catalogue;

/// <summary>
/// Which way a diagram's flow runs (<see cref="Diagram.Flow"/>): where the next step goes when one is added from the
/// sidebar, a number key or "+ Next step". Swim lanes map each direction to a lane layout (<c>LaneLayouts.ForFlow</c>).
/// </summary>
public static class FlowDirections
{
    public const string Right = "right", Down = "down";
}
