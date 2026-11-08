namespace BeDiagram.Models.Shapes;

/// <summary>The registered outlines, by shape key. Unknown keys draw as a plain rectangle.</summary>
public static class ShapeOutlines
{
    /// <summary>Used for unknown keys, and for text boxes and groups when a connector is clipped to them.</summary>
    public static readonly IShapeOutline Rect = new RectOutline();

    static readonly Dictionary<string, IShapeOutline> ByKey = new IShapeOutline[]
    {
        Rect, new CheckOutline(), new RoundedRectOutline(), new PillOutline(), new DiamondOutline(),
        new ParallelogramOutline(), new HexagonOutline(), new CircleOutline(),
        new SealOutline(), new OctagonOutline(), new TriangleOutline(), new RoundedSquareOutline(), new PanelOutline(),
    }.ToDictionary(o => o.Key);

    public static IShapeOutline Get(string? key) =>
        key is not null && ByKey.TryGetValue(key, out IShapeOutline? outline) ? outline : Rect;

    public static IReadOnlyCollection<IShapeOutline> All => ByKey.Values;
}
