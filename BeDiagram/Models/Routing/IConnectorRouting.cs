using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;

namespace BeDiagram.Models.Routing;

/// <summary>
/// Strategy pattern: how a connector of one <see cref="EdgeStyle"/> runs between two boxes. <see cref="ElbowRouting"/>
/// makes right-angled routes (automatic sides, pinned sides, a dragged bend); <see cref="StraightRouting"/> a direct line.
/// <see cref="EdgeRouter.Route(IBox, IBox, EdgeStyle, Side?, Side?, double)"/> picks the strategy for the style, after
/// keeping row ends on their left / right side. A new connector style is a new class here.
/// </summary>
public interface IConnectorRouting
{
    /// <summary>The connector style this routing draws.</summary>
    EdgeStyle Style { get; }

    /// <summary>The route from <paramref name="a"/> to <paramref name="b"/>; a null side is chosen automatically,
    /// <paramref name="bend"/> is how far the middle segment has been dragged (elbow only).</summary>
    EdgeRoute Route(IBox a, IBox b, Side? fromSide, Side? toSide, double bend);
}
