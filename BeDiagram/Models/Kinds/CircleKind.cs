using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Kinds;

/// <summary>The Circle type: a circle with an icon in the middle and the label underneath (presentation style).</summary>
public class CircleKind : NodeKind
{
    public override bool DrawnAsCircle(Node n) => true;

    /// <summary>A new circle says "Label" rather than "Circle"; a retyped one still takes the type's name.</summary>
    public override string NewLabel(Node n) => "Label";

    /// <summary>A circle always has an icon: a retyped one without one gets the type's placeholder.</summary>
    public override void Adopt(Node n)
    {
        base.Adopt(n);
        n.Icon ??= NodeTypes.Get(NodeType.Circle).Icon;
    }
}
