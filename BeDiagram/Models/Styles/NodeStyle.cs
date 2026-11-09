using BeDiagram.Models.Emphasis;

namespace BeDiagram.Models.Styles;

/// <summary>Resolved colours for drawing a node.</summary>
public record NodeStyle(string Fill, string Stroke, string Text, string IconColor)
{
    /// <summary>The node's colours (from its kind, see <see cref="Kinds.INodeKind.Colours"/>) with its emphasis applied
    /// (see <see cref="IEmphasis"/>).</summary>
    public static NodeStyle For(Node n)
    {
        IEmphasis em = Emphases.Of(n);
        return em.Adjust(n.Kind.Colours(n, n.Filled || em.ForcesSolidFill));
    }
}
