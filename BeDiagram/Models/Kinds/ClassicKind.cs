using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Kinds;

/// <summary>The eight classic shapes (start, process, decision, check, action, data, status, end): their type's own
/// outline, or a circle when <see cref="Node.Round"/> is set.</summary>
public class ClassicKind : NodeKind
{
    public override bool CanBeRound => true;

    public override string ShapeOf(Node n) => n.Round ? NodeTypes.Get(NodeTypes.Circle).Shape : base.ShapeOf(n);

    public override bool DrawnAsCircle(Node n) => n.Round;
}
