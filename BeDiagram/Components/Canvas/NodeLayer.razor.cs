using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Text;
using BeDiagram.Rendering;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Components.Canvas;

/// <summary>Shapes: selection outline, halo, shadow, outline, side stripe, list / IPO body, icon, label (inline editor while editing), description, property dot, link and step badges, and ports (rows of a list / IPO box have their own).</summary>
public partial class NodeLayer : CanvasLayer
{

    IEnumerable<NodeView> NodeViews() =>
        D.Nodes.Select(n =>
        {
            LabelLayout lay = TextLayout.Layout(n);
            Rect? back = TextLayout.DrawnTextBack(n, lay) is { } r ? new Rect(r.X, r.Y, r.W, r.H) : null;
            return new NodeView(n, NodeTypes.Get(n.Type), n.Kind.OutlineOf(n), NodeStyle.For(n), Editor.Selection.Contains(n.Id), lay, TextLayout.ExtraBelow(n), back);
        });

    /// <summary>The inside of a list / IPO node (title band, sections, rows) — same markup as the export — or null.</summary>
    static MarkupString? PanelBody(NodeView v) => v.N.Kind.Body(v.N, v.Style) is { } body ? (MarkupString)body : null;

    static string NodeIconTransform(LabelLayout lay) =>
        $"translate({F(lay.IconX)},{F(lay.IconY)}) scale({F(lay.IconSize / 24)})";

    static MarkupString NodeIconMarkup(Node n) => (MarkupString)Icons.Get(n.Kind.IconOf(n));

    /// <summary>The halo ring around a major step (same markup as the export), or null.</summary>
    static MarkupString? HaloMarkup(NodeView v) => SvgShapes.HaloSvg(v.N, v.Style.Stroke) is { } h ? (MarkupString)h : null;

    /// <summary>The step-number badge on a shape (same markup as the export).</summary>
    static MarkupString StepBadgeMarkup(NodeView v, int step) => (MarkupString)SvgShapes.StepBadgeSvg(v.N, step, v.Style.Stroke);

    /// <summary>A node's label lines with the tspan line offset (first line 0, then one line height each).</summary>
    static IEnumerable<(string Text, string Dy)> LabelLines(LabelLayout lay) =>
        lay.Lines.Select((line, i) => (line, i == 0 ? "0" : F(TextLayout.LineHeight)));

    /// <summary>Description lines with the tspan line offset.</summary>
    static IEnumerable<(string Text, string Dy)> DescLines(LabelLayout lay) =>
        lay.Desc.Select((line, i) => (line, i == 0 ? "0" : F(TextLayout.DescLineHeight)));

    /// <summary>Where the inline label editor sits: under the shape for labels drawn below, otherwise inside it.</summary>
    static Rect NodeEditRect(NodeView v) =>
        v.N.Kind.LabelEditRect(v.N) is { } own ? own
        : v.Layout.Below
        ? new Rect(v.N.X + v.N.W / 2 - 80, v.N.Y + v.N.H + 4, 160, 44)
        : new Rect(v.N.X + 8, v.N.Y + 6, v.N.W - 16, v.N.H - 12);

    static bool HasProperties(Node n) => n.Properties.Values.Any(v => !string.IsNullOrWhiteSpace(v));
}
