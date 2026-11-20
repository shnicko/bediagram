using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Serialization;
using BeDiagram.Models.Styles;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Models.Heads;

/// <summary>
/// A head drawn from SVG shapes in its own small box (<see cref="Width"/> × <see cref="Height"/>) pointing right,
/// with its tip at the right-hand middle. Subclasses give only the size and the shapes; the marker, its size per
/// line weight and the preview come from here (Template Method).
/// </summary>
public abstract class MarkerHead : IArrowHead
{
    /// <summary>Stroke width of the head's lines, in the head's own units.</summary>
    protected const double Stroke = 1.5;

    public abstract ArrowHead Key { get; }

    public abstract string Name { get; }

    public abstract string Hint { get; }

    public abstract string Family { get; }

    /// <summary>Length of the head along the line.</summary>
    protected abstract double Width { get; }

    /// <summary>Height of the head across the line.</summary>
    protected abstract double Height { get; }

    /// <summary>The head's SVG shapes in its own box, in <paramref name="colour"/>.</summary>
    protected abstract string Shapes(string colour);

    public virtual string? MarkerId(string colour, string weight)
    {
        return $"hd-{Saved()}-{colour.TrimStart('#')}-{weight}";
    }

    /// <summary>Sized in user units, a little smaller on light lines and larger on heavy ones; turned to follow the
    /// line at either end (auto-start-reverse), so one definition serves both ends.</summary>
    public virtual string MarkerDef(string colour, string weight)
    {
        double k = Scale(weight);
        return $"<marker id=\"{MarkerId(colour, weight)}\" viewBox=\"0 0 {F(Width)} {F(Height)}\" refX=\"{F(Width)}\" refY=\"{F(Height / 2)}\" "
             + $"markerUnits=\"userSpaceOnUse\" markerWidth=\"{F(Width * k)}\" markerHeight=\"{F(Height * k)}\" orient=\"auto-start-reverse\" overflow=\"visible\">"
             + Shapes(colour) + "</marker>";
    }

    public virtual string Preview(double tipX, double y, string colour)
    {
        return $"<g transform=\"translate({F(tipX - Width)},{F(y - Height / 2)})\">{Shapes(colour)}</g>";
    }

    /// <summary>How much bigger than its own box the head is drawn on a line of this weight.</summary>
    static double Scale(string weight)
    {
        switch (weight)
        {
            case LineLook.Light:
                return 0.8;
            case LineLook.Heavy:
                return 1.25;
            default:
                return 1;
        }
    }

    string Saved()
    {
        return EnumNames.Of(Key);
    }

    /// <summary>A stroked path with no fill.</summary>
    protected static string Line(string d, string colour)
    {
        return $"<path d=\"{d}\" fill=\"none\" stroke=\"{colour}\" stroke-width=\"{F(Stroke)}\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>";
    }

    /// <summary>A closed path filled with <paramref name="fill"/> and outlined.</summary>
    protected static string Shape(string d, string fill, string colour)
    {
        return $"<path d=\"{d}\" fill=\"{fill}\" stroke=\"{colour}\" stroke-width=\"{F(Stroke)}\" stroke-linejoin=\"miter\"/>";
    }

    /// <summary>The crow's-foot ring ("zero"), filled white to hide the line through it.</summary>
    protected static string Ring(double cx, double cy, double r, string colour)
    {
        return $"<circle cx=\"{F(cx)}\" cy=\"{F(cy)}\" r=\"{F(r)}\" fill=\"#ffffff\" stroke=\"{colour}\" stroke-width=\"{F(Stroke)}\"/>";
    }
}
