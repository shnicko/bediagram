using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Text;
using BeDiagram.Rendering;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Components.Canvas;

/// <summary>Shapes: selection outline, halo, shadow, outline, side stripe, list / IPO body, icon, label (inline editor while editing), description, property dot, link and step badges, and ports (rows of a list / IPO box have their own).</summary>
public partial class NodeLayer : CanvasLayer
{

    List<NodeView> NodeViews()
    {
        List<NodeView> views = new List<NodeView>();
        foreach (Node n in D.Nodes)
        {
            LabelLayout lay = TextLayout.Layout(n);
            Rect? back = null;
            (double X, double Y, double W, double H)? r = TextLayout.DrawnTextBack(n, lay);
            if (r is not null)
            {
                back = new Rect(r.Value.X, r.Value.Y, r.Value.W, r.Value.H);
            }
            Rect? outside = null;
            (double X, double Y, double W, double H)? o = TextLayout.OutsideTextBox(n, lay);
            if (o is not null)
            {
                outside = new Rect(o.Value.X, o.Value.Y, o.Value.W, o.Value.H);
            }
            views.Add(new NodeView(n, NodeTypes.Get(n.Type), n.Kind.OutlineOf(n), NodeStyle.For(n), Editor.Selection.Contains(n.Id), lay, TextLayout.ExtraBelow(n), back, outside));
        }
        return views;
    }

    /// <summary>The inside of a list / IPO node (title band, sections, rows) — same markup as the export — or null.</summary>
    static MarkupString? PanelBody(NodeView v)
    {
        string? body = v.N.Kind.Body(v.N, v.Style);
        if (body is null)
        {
            return null;
        }
        return (MarkupString)body;
    }

    static string NodeIconTransform(LabelLayout lay)
    {
        return $"translate({F(lay.IconX)},{F(lay.IconY)}) scale({F(lay.IconSize / 24)})";
    }

    static MarkupString NodeIconMarkup(Node n)
    {
        return (MarkupString)Icons.Get(n.Kind.IconOf(n));
    }

    /// <summary>The halo ring around a major step (same markup as the export), or null.</summary>
    static MarkupString? HaloMarkup(NodeView v)
    {
        string? h = SvgShapes.HaloSvg(v.N, v.Style.Stroke);
        if (h is null)
        {
            return null;
        }
        return (MarkupString)h;
    }

    /// <summary>The step-number badge on a shape (same markup as the export).</summary>
    static MarkupString StepBadgeMarkup(NodeView v, int step)
    {
        return (MarkupString)SvgShapes.StepBadgeSvg(v.N, step, v.Style.Stroke);
    }

    /// <summary>A node's label lines with the tspan line offset (first line 0, then one line height each).</summary>
    static List<(string Text, string Dy)> LabelLines(LabelLayout lay)
    {
        List<(string Text, string Dy)> lines = new List<(string Text, string Dy)>();
        for (int i = 0; i < lay.Lines.Count; i++)
        {
            string dy = F(TextLayout.LineHeight);
            if (i == 0)
            {
                dy = "0";
            }
            lines.Add((lay.Lines[i], dy));
        }
        return lines;
    }

    /// <summary>Description lines with the tspan line offset.</summary>
    static List<(string Text, string Dy)> DescLines(LabelLayout lay)
    {
        List<(string Text, string Dy)> lines = new List<(string Text, string Dy)>();
        for (int i = 0; i < lay.Desc.Count; i++)
        {
            string dy = F(TextLayout.DescLineHeight);
            if (i == 0)
            {
                dy = "0";
            }
            lines.Add((lay.Desc[i], dy));
        }
        return lines;
    }

    /// <summary>Where the inline label editor sits: under the shape for labels drawn below, otherwise inside it.</summary>
    static Rect NodeEditRect(NodeView v)
    {
        Rect? own = v.N.Kind.LabelEditRect(v.N);
        if (own is not null)
        {
            return own;
        }
        if (v.Layout.Below)
        {
            // Follows the label when it has been moved to another side or dragged free.
            return new Rect(v.N.X + v.N.W / 2 - 80 + v.Layout.ShiftX, v.N.Y + v.N.H + 4 + v.Layout.ShiftY, 160, 44);
        }
        return new Rect(v.N.X + 8, v.N.Y + 6, v.N.W - 16, v.N.H - 12);
    }

    /// <summary>The leader from dragged text back to the shape (same markup as the export), or null.</summary>
    static MarkupString? LeaderMarkup(NodeView v)
    {
        if (v.Layout.Leader is null)
        {
            return null;
        }
        return (MarkupString)SvgShapes.LeaderSvg(v.Layout.Leader);
    }

    /// <summary>The label's colour: the darker "below" colour when it sits outside the shape, else the style's text colour.</summary>
    static string LabelFill(NodeView v)
    {
        if (v.Layout.Below)
        {
            return Emphases.Of(v.N).BelowLabelColour;
        }
        return v.Style.Text;
    }

    /// <summary>A press on text that has been freed from its shape (only free text can be picked up): drag it.</summary>
    void OutsideDown(PointerEventArgs e, NodeView v)
    {
        Input.OutsideTextDown(e, v.N.Id);
    }

    static bool HasProperties(Node n)
    {
        foreach (string value in n.Properties.Values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return true;
            }
        }
        return false;
    }
}
