using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Shapes;

namespace BeDiagram.Models.Kinds;

/// <summary>The eight classic shapes (start, process, decision, check, action, data, status, end): their type's own
/// outline, or a circle when <see cref="Node.Round"/> is set.</summary>
public class ClassicKind : NodeKind
{
    public override bool CanBeRound => true;

    public override IShapeOutline OutlineOf(Node n) => n.Round ? ShapeOutlines.Circle : base.OutlineOf(n);

    public override bool DrawnAsCircle(Node n) => n.Round;
}
