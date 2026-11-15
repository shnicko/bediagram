namespace BeDiagram.Models.Layout;

/// <summary>Where a node's label, icon and description go. Shared by the canvas and the SVG export.</summary>
public record LabelLayout(List<string> Lines, double TextX, double TextY, bool Below,
                          bool HasIcon, double IconX, double IconY, double IconSize)
{
    /// <summary>Description lines (smaller text). Empty when there is no description.</summary>
    public List<string> Desc { get; init; } = new();
    /// <summary>Centre of the description lines (the shape's centre).</summary>
    public double DescX { get; init; }
    /// <summary>Baseline of the first description line.</summary>
    public double DescY { get; init; }
    /// <summary>How far the text outside the shape was moved from its usual place under the shape: to another side
    /// (<see cref="Node.LabelSide"/>) or to where the user dragged it (<see cref="Node.FreeLabel"/>). 0 when it sits underneath.</summary>
    public double ShiftX { get; init; }
    public double ShiftY { get; init; }
    /// <summary>The leader from dragged-away text back to the shape, or null when the text is attached or close to the shape.</summary>
    public LeaderLine? Leader { get; init; }
}
