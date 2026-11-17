namespace BeDiagram.Models.Location;

/// <summary>The placement objects, one of each (they hold no state). Items hand out theirs as <c>Placement</c>; use
/// that rather than these, so the item decides.</summary>
public static class Placements
{
    public static readonly IPlacement Shape = new ShapePlacement();
    public static readonly IPlacement Panel = new PanelPlacement();
    public static readonly IPlacement Corner = new CornerPlacement();
    public static readonly IPlacement TextBox = new TextBoxPlacement();
    public static readonly IPlacement Group = new GroupPlacement();
}
