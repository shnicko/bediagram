using System.Globalization;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Emphasis;

/// <summary>Base emphasis with the normal look; <see cref="MajorEmphasis"/> and <see cref="MinorEmphasis"/> override what differs.</summary>
public abstract class NodeEmphasis : IEmphasis
{
    public abstract string Key { get; }
    public abstract string Name { get; }
    public abstract string Hint { get; }
    public virtual bool ForcesSolidFill => false;
    public virtual NodeStyle Adjust(NodeStyle normal) => normal;
    public virtual double StrokeWidth => Geometry.ShapeStroke;
    public virtual double ShadowOpacity => Geometry.ShadowOpacity;
    public virtual bool HasHalo => false;

    /// <summary>Circles get semibold (600) so their labels read well on slides; others stay regular.
    /// 600 rather than 500 because Segoe UI has no 500 and would fall back to regular.</summary>
    public virtual string? LabelWeight(Node n) => n.Type == NodeTypes.Circle ? "600" : null;

    public virtual string BelowLabelColour => "#1f2933";

    /// <summary>A colour mixed towards white: 0 = unchanged, 1 = white. Used for the lighter minor look.</summary>
    protected static string Lighten(string hex, double t)
    {
        if (hex.Length != 7 || hex[0] != '#') return hex;
        int Mix(int i) => (int)Math.Round(int.Parse(hex.Substring(i, 2), NumberStyles.HexNumber) * (1 - t) + 255 * t);
        return $"#{Mix(1):x2}{Mix(3):x2}{Mix(5):x2}";
    }
}
