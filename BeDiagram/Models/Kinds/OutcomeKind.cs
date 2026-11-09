using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Shapes;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Kinds;

/// <summary>
/// Outcome nodes (pass, fail, rework, conditional, hold): the result of a step, not a step. The outcome
/// (<see cref="Node.Outcome"/>, see <see cref="Outcomes"/>) sets the outline, the bold glyph and the colour, so it reads
/// the same in greyscale. The label always sits underneath.
/// </summary>
public class OutcomeKind : NodeKind
{
    public override bool IsStep => false;
    public override bool HasOutcome => true;
    public override double IconStroke => 2.75;

    public override IShapeOutline OutlineOf(Node n) => Outcomes.Get(n.Outcome).Outline;

    public override string? IconOf(Node n) => Outcomes.Get(n.Outcome).Glyph;

    public override bool LabelBelow(Node n) => true;

    /// <summary>Glyph centred in the outline, label underneath. A triangle's visual centre is lower down.</summary>
    public override LabelLayout LayoutLabel(Node n, string label)
    {
        IShapeOutline outline = Geometry.OutlineOf(n);
        double cx = n.X + n.W / 2;
        double size = Math.Min(n.W, n.H) * outline.GlyphScale;
        double gy = n.Y + n.H * outline.GlyphCentreY;
        List<string> lines = Geometry.Wrap(label, Geometry.TextWidth(n));
        return new(lines, cx, n.Y + n.H + Geometry.HaloOut(n) + 18, true, true, cx - size / 2, gy - size / 2, size);
    }

    /// <summary>Outcomes default to their own colour (green pass, red fail, ...). Solid with no palette colour,
    /// conditional uses a dark mark on amber, like a warning sign: white on amber is too faint in greyscale.</summary>
    public override NodeStyle Colours(Node n, bool filled)
    {
        Swatch os = Palette.Get(Outcomes.Get(n.Outcome).Color);
        if (n.Color is null && filled)
            return new NodeStyle(os.Solid, os.Solid, "#ffffff", n.Outcome == OutcomeType.Conditional ? "#1f2933" : "#ffffff");
        return Painted(n, filled, os.Light, os.Stroke);
    }

    public override string NameOf(Node n) => Outcomes.Get(n.Outcome).Display;

    public override string PanelTitle(Node n) => $"Outcome · {NameOf(n)}";

    /// <summary>A new outcome is the tile's outcome (pass when there's none), drawn solid.</summary>
    public override void Init(Node n, OutcomeType? outcome)
    {
        n.Outcome = outcome ?? OutcomeType.Pass;
        n.Filled = true;
    }

    /// <summary>A shape retyped to an outcome keeps the outcome it had, or becomes a pass, drawn solid.</summary>
    public override void Adopt(Node n)
    {
        n.Outcome ??= OutcomeType.Pass;
        n.Filled = true;
    }
}
