using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Shapes;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Text;

namespace BeDiagram.Models.Kinds;

/// <summary>
/// Base node kind with classic-shape behaviour: a step, drawn with its type's outline, the chosen icon, label laid out
/// by the outline, the type's colours. Subclasses override what differs.
/// </summary>
public abstract class NodeKind : INodeKind
{
    public virtual bool IsStep => true;
    public virtual bool CanBeRound => false;
    public virtual bool IsPanel => false;
    public virtual bool HasOutcome => false;
    public virtual string? PaletteInnerPath => null;
    public virtual double IconStroke => 2;

    public virtual IShapeOutline OutlineOf(Node n) => NodeTypes.Get(n.Type).Outline;

    public virtual bool DrawnAsCircle(Node n) => false;

    public virtual string? IconOf(Node n) => n.Icon;

    public virtual bool LabelBelow(Node n) => n.Kind.OutlineOf(n).IconMovesLabelBelow && Icons.Exists(n.Icon);

    public virtual LabelLayout LayoutLabel(Node n, string label)
    {
        IShapeOutline outline = n.Kind.OutlineOf(n);
        double cx = n.X + n.W / 2, cy = n.Y + n.H / 2;
        bool icon = Icons.Exists(n.Icon);

        if (icon && outline.IconMovesLabelBelow)
        {
            // Presentation style: icon centred in the circle, label underneath.
            double size = Math.Min(n.W, n.H) * 0.46;
            List<string> lines = TextLayout.Wrap(label, TextLayout.TextWidth(n));
            return new(lines, cx, n.Y + n.H + Boxes.HaloOut(n) + 18, true, true, cx - size / 2, cy - size / 2, size);
        }

        if (icon && outline.IconAboveTextWrap is double aboveWrap)
        {
            // Narrow-sided shapes: icon above the text.
            List<string> lines = TextLayout.Wrap(label, n.W * aboveWrap);
            double block = 22 + lines.Count * TextLayout.LineHeight;
            double top = cy - block / 2;
            return new(lines, cx, top + 22 + 12, false, true, cx - 10, top, 20);
        }

        if (icon)
        {
            // Icon on the left, text centred in the remaining space.
            double left = n.X + outline.IconInset;
            List<string> lines = TextLayout.Wrap(label, n.X + n.W - (left + 26) - 10);
            double textX = (left + 26 + n.X + n.W - 8) / 2;
            return new(lines, textX, cy - (lines.Count - 1) * TextLayout.LineHeight / 2 + 4, false, true, left, cy - 10, 20);
        }

        List<string> wrapped = TextLayout.Wrap(label, n.W * outline.TextWrap);
        return new(wrapped, cx, cy - (wrapped.Count - 1) * TextLayout.LineHeight / 2 + 4, false, false, 0, 0, 0);
    }

    public virtual NodeStyle Colours(Node n, bool filled)
    {
        NodeTypeInfo t = NodeTypes.Get(n.Type);
        return Painted(n, filled, t.Fill, t.Stroke);
    }

    /// <summary>The node's palette colour (if any) over the kind's own <paramref name="fill"/> / <paramref name="stroke"/>,
    /// solid when <paramref name="filled"/>.</summary>
    protected NodeStyle Painted(Node n, bool filled, string fill, string stroke)
    {
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

    public virtual string NameOf(Node n) => NodeTypes.Get(n.Type).Display;

    public virtual string PanelTitle(Node n) => NameOf(n);

    public virtual string NewLabel(Node n) => NameOf(n);

    public virtual string RetypeLabel(Node n) => NameOf(n);

    public virtual (double W, double H) MinSize(Node n) => (80.0, 40.0);

    public virtual Rect? LabelEditRect(Node n) => null;

    public virtual string? Body(Node n, NodeStyle style) => null;

    public virtual IBox EndAt(Node n, string? itemId) => n;

    public virtual ItemBox? RowAt(Node n, double x, double y) => null;

    public virtual IEnumerable<(double X, double Y, string ItemId)> RowPorts(Node n) =>
        Enumerable.Empty<(double X, double Y, string ItemId)>();

    public virtual List<ListItem>? NewItems() => null;

    public virtual void Init(Node n, OutcomeType? outcome) { }

    public virtual void Adopt(Node n) => n.Outcome = null;
}
