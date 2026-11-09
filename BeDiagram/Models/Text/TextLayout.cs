using System.Text;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Shapes;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Text;

/// <summary>Text on the drawing: node labels and descriptions (inside or under the shape), text boxes, wrapping, and the room text takes under a shape. Pure functions shared by the canvas, the export and the editor.</summary>
public static class TextLayout
{

    public const double LineHeight = 16;
    public const double DescFontSize = 11;
    public const double DescLineHeight = 14;
    /// <summary>Average character width of the 11px description text (for wrapping).</summary>
    const double DescCharWidth = 6.1;


    /// <summary>The label shown for a node: its own label, or the type name when it has none.</summary>
    public static string LabelText(Node n) => string.IsNullOrWhiteSpace(n.Label) ? NodeTypes.Get(n.Type).Display : n.Label;

    /// <summary>
    /// Wrap width for text drawn outside the shape (a label under a circle/outcome, and the description).
    /// The node's own <see cref="Node.TextWidth"/> when set, otherwise a little wider than the shape.
    /// </summary>
    public static double TextWidth(Node n) =>
        n.TextWidth is > 0 ? n.TextWidth.Value : DefaultTextWidth(n);

    public static double DefaultTextWidth(Node n) => n.Kind.LabelBelow(n) ? Math.Max(n.W * 1.7, 130) : Math.Max(n.W, 160);

    public static LabelLayout Layout(Node n) => Layout(n, LabelText(n));

    public static LabelLayout Layout(Node n, string label)
    {
        LabelLayout lay = n.Kind.LayoutLabel(n, label);
        if (n.Description is not { } desc) return lay;
        // Under the label when the label is below the shape; otherwise just under the shape.
        List<string> lines = Wrap(desc, TextWidth(n), DescCharWidth);
        double firstBaseline = lay.Below
            ? lay.TextY + (lay.Lines.Count - 1) * LineHeight + 4 + DescLineHeight
            : n.Y + n.H + Boxes.HaloOut(n) + 6 + DescLineHeight;
        return lay with { Desc = lines, DescX = n.X + n.W / 2, DescY = firstBaseline };
    }

    public static TextBoxLayout Layout(TextBox t)
    {
        double size = Math.Max(4, t.FontSize);
        double lh = size * TextLook.LineSpacing;
        double charW = size * TextLook.CharWidth(t.Font) * (t.Bold ? 1.06 : 1);
        List<string> lines = Wrap(t.Text, Math.Max(10, t.W - 2 * TextLook.PadX), charW);
        double block = lines.Count * lh;
        double top = t.VAlign switch
        {
            TextLook.Middle => t.Y + (t.H - block) / 2,
            TextLook.Bottom => t.Y + t.H - TextLook.PadY - block,
            _ => t.Y + TextLook.PadY,
        };
        (double x, string? anchor) = t.Align switch
        {
            TextLook.Center => (t.X + t.W / 2, "middle"),
            TextLook.Right => (t.X + t.W - TextLook.PadX, "end"),
            _ => (t.X + TextLook.PadX, "start"),
        };
        // Baseline sits roughly 0.8 em below the top of the line box.
        double first = top + (lh - size) / 2 + size * 0.8;
        List<TextLine> placed = lines.Select((l, i) => new TextLine(l, x, first + i * lh)).ToList();
        return new TextBoxLayout(placed, anchor, lh, block + 2 * TextLook.PadY);
    }

    /// <summary>The white backing to draw behind text outside the shape, or null when there is none or the node
    /// asks for a clear background (<see cref="Node.ClearTextBack"/>). Spacing and bounds still use
    /// <see cref="OutsideTextBox"/>, so a clear background doesn't move anything.</summary>
    public static (double X, double Y, double W, double H)? DrawnTextBack(Node n, LabelLayout lay) =>
        n.ClearTextBack ? null : OutsideTextBox(n, lay);

    /// <summary>
    /// The white backing behind text drawn outside the shape (label below and/or description), or null when nothing
    /// is drawn outside. One rectangle covers both so they read as one block.
    /// </summary>
    public static (double X, double Y, double W, double H)? OutsideTextBox(Node n, LabelLayout lay)
    {
        if (!lay.Below && lay.Desc.Count == 0) return null;
        double cx = n.X + n.W / 2;
        double w = 0, top, bottom;
        if (lay.Below)
        {
            w = lay.Lines.Max(l => l.Length) * 7.2 + 14;
            top = lay.TextY - 14;
            bottom = top + lay.Lines.Count * LineHeight + 6;
        }
        else
        {
            top = n.Y + n.H + Boxes.HaloOut(n) + 4;
            bottom = top;
        }
        if (lay.Desc.Count > 0)
        {
            w = Math.Max(w, lay.Desc.Max(l => l.Length) * DescCharWidth + 14);
            bottom = lay.DescY + (lay.Desc.Count - 1) * DescLineHeight + 5;
        }
        return (cx - w / 2, top, w, bottom - top);
    }

    /// <summary>
    /// Space taken under a box by text drawn outside it: the label under a circle/outcome and the description.
    /// A one-line label under a circle takes 34px (as before descriptions existed); more lines take more.
    /// </summary>
    public static double ExtraBelow(IBox b)
    {
        if (b is Group g) return g.TitleBelowBox() ? Group.Header : 0;
        if (b is not Node n || (!n.Kind.LabelBelow(n) && n.Description is null)) return 0;
        LabelLayout lay = Layout(n);
        return OutsideTextBox(n, lay) is { } r ? r.Y + r.H - (n.Y + n.H) + 8 : 0;
    }

    /// <summary>Very simple word wrap for SVG text, based on an average character width.</summary>
    public static List<string> Wrap(string text, double width, double charWidth = 7.2)
    {
        int maxChars = Math.Max(4, (int)(width / charWidth));
        List<string> lines = new List<string>();
        foreach (string para in (text ?? "").Replace("\r", "").Split('\n'))
        {
            StringBuilder sb = new StringBuilder();
            foreach (string word in para.Split(' '))
            {
                if (sb.Length > 0 && sb.Length + 1 + word.Length > maxChars)
                {
                    lines.Add(sb.ToString());
                    sb.Clear();
                }
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(word);
            }
            lines.Add(sb.ToString());
        }
        return lines;
    }
}
