using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Emphasis;

/// <summary>A supporting step: white fill, a lighter thinner outline, lighter icon and muted text, no shadow.</summary>
public class MinorEmphasis : NodeEmphasis
{
    public override EmphasisLevel Level => EmphasisLevel.Minor;
    public override string Name => "Minor";
    public override string Hint => "A supporting step: lighter outline and text, no shadow";

    public override NodeStyle Adjust(NodeStyle normal) =>
        new NodeStyle("#ffffff", Lighten(normal.Stroke, 0.4), MutedText, Lighten(normal.Stroke, 0.3));

    public override double StrokeWidth => 1.6;
    public override double ShadowOpacity => 0;
    public override string? LabelWeight(Node n) => null;
    public override string BelowLabelColour => MutedText;

    const string MutedText = "#5b6573";
}
