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
}
