namespace BeDiagram.Models.Shapes;

/// <summary>
/// One shared instance of each outline (they hold no state). Node types and outcomes point at these directly
/// (<see cref="Catalogue.NodeTypeInfo.Outline"/>, <see cref="Catalogue.OutcomeInfo.Outline"/>), so there is no
/// lookup by name.
/// </summary>
public static class ShapeOutlines
{
    /// <summary>Also used for text boxes and groups when a connector is clipped to them.</summary>
    public static readonly IShapeOutline Rect = new RectOutline();
    public static readonly IShapeOutline Check = new CheckOutline();
    public static readonly IShapeOutline RoundedRect = new RoundedRectOutline();
    public static readonly IShapeOutline Pill = new PillOutline();
    public static readonly IShapeOutline Diamond = new DiamondOutline();
    public static readonly IShapeOutline Parallelogram = new ParallelogramOutline();
    public static readonly IShapeOutline Hexagon = new HexagonOutline();
    public static readonly IShapeOutline Circle = new CircleOutline();
    public static readonly IShapeOutline Seal = new SealOutline();
    public static readonly IShapeOutline Octagon = new OctagonOutline();
    public static readonly IShapeOutline Triangle = new TriangleOutline();
    public static readonly IShapeOutline RoundedSquare = new RoundedSquareOutline();
    public static readonly IShapeOutline Panel = new PanelOutline();
}
