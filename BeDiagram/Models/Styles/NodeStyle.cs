using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;

namespace BeDiagram.Models.Styles;

/// <summary>Resolved colours for drawing a node.</summary>
public record NodeStyle(string Fill, string Stroke, string Text, string IconColor)
{
    /// <summary>The node's colours with its emphasis applied (see <see cref="IEmphasis"/>).</summary>
    public static NodeStyle For(Node n)
    {
        IEmphasis em = Emphases.Of(n);
        return em.Adjust(Base(n, n.Filled || em.ForcesSolidFill));
    }

    /// <summary>Colours from the type, outcome, palette colour and fill, before emphasis.</summary>
    static NodeStyle Base(Node n, bool filled)
    {
        NodeTypeInfo t = NodeTypes.Get(n.Type);
        string fill = t.Fill, stroke = t.Stroke;
        if (n.Type == NodeTypes.Outcome)
        {
            // Outcomes default to their own colour (green pass, red fail, ...).
            Swatch os = Palette.Get(Outcomes.Get(n.Outcome).Color);
            fill = os.Light; stroke = os.Stroke;
            // Conditional uses a dark mark on amber, like a warning sign: white on amber is too faint in greyscale.
            if (n.Color is null && filled)
                return new NodeStyle(os.Solid, os.Solid, "#ffffff", n.Outcome == Outcomes.Conditional ? "#1f2933" : "#ffffff");
        }
        if (n.Color is not null && Palette.All.TryGetValue(n.Color, out Swatch? sw))
        {
            fill = sw.Light; stroke = sw.Stroke;
            if (filled) return new NodeStyle(sw.Solid, sw.Solid, "#ffffff", "#ffffff");
        }
        else if (filled)
        {
            return new NodeStyle(stroke, stroke, "#ffffff", "#ffffff");
        }
        return new NodeStyle(fill, stroke, "#1f2933", stroke);
    }
}
