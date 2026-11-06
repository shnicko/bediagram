namespace BeDiagram.Models.Emphasis;

/// <summary>A key step: solid fill, white icon and text, and a thin ring around the outline, a little way out.</summary>
public class MajorEmphasis : NodeEmphasis
{
    public override string Key => Emphases.Major;
    public override string Name => "Major";
    public override string Hint => "A key step: solid fill with a ring around it";
    public override bool ForcesSolidFill => true;
    public override bool HasHalo => true;

    /// <summary>Every major label is semibold, whatever the shape.</summary>
    public override string? LabelWeight(Node n) => "600";
}
