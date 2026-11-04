using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Styles;

/// <summary>Resolved colours for drawing a node.</summary>
public record NodeStyle(string Fill, string Stroke, string Text, string IconColor)
{
    public static NodeStyle For(Node n)
    {
        NodeTypeInfo t = NodeTypes.Get(n.Type);
        string fill = t.Fill, stroke = t.Stroke;
        if (n.Type == NodeTypes.Outcome)
        {
            // Outcomes default to their own colour (green pass, red fail, ...).
            Swatch os = Palette.Get(Outcomes.Get(n.Outcome).Color);
            fill = os.Light; stroke = os.Stroke;
            // Conditional uses a dark mark on amber, like a warning sign: white on amber is too faint in greyscale.
            if (n.Color is null && n.Filled)
                return new NodeStyle(os.Solid, os.Solid, "#ffffff", n.Outcome == Outcomes.Conditional ? "#1f2933" : "#ffffff");
        }
        if (n.Color is not null && Palette.All.TryGetValue(n.Color, out Swatch? sw))
        {
            fill = sw.Light; stroke = sw.Stroke;
            if (n.Filled) return new NodeStyle(sw.Solid, sw.Solid, "#ffffff", "#ffffff");
        }
        else if (n.Filled)
        {
            return new NodeStyle(stroke, stroke, "#ffffff", "#ffffff");
        }
        return new NodeStyle(fill, stroke, "#1f2933", stroke);
    }
}
