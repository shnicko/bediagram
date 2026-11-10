using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages.Gestures;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Routing;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Text;
using BeDiagram.Rendering;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Components.Pages;

/// <summary>Values the markup reads: view records for nodes, connectors, groups and text boxes, palette tiles, tooltips and panel text.</summary>
public partial class Designer
{
    // ───────────── Render helpers (read by Designer.razor) ─────────────
    // The markup only loops and reads values; anything that needs working out lives here.


    /// <summary>The inside of a list / IPO node (title band, sections, rows) — same markup as the export — or null.</summary>
    static MarkupString? PanelBody(NodeView v) => v.N.Kind.Body(v.N, v.Style) is { } body ? (MarkupString)body : null;


    double BendHandleSize => 12 / Zoom;

    static string NodeIconTransform(LabelLayout lay) =>
        $"translate({F(lay.IconX)},{F(lay.IconY)}) scale({F(lay.IconSize / 24)})";

    static MarkupString NodeIconMarkup(Node n) => (MarkupString)Icons.Get(n.Kind.IconOf(n));

    /// <summary>The halo ring around a major step (same markup as the export), or null.</summary>
    static MarkupString? HaloMarkup(NodeView v) => SvgShapes.HaloSvg(v.N, v.Style.Stroke) is { } h ? (MarkupString)h : null;

    /// <summary>Major steps are always solid, so the panel's Solid fill box is ticked and greyed out.</summary>

    /// <summary>The step-number badge on a shape (same markup as the export).</summary>
    static MarkupString StepBadgeMarkup(NodeView v, int step) => (MarkupString)SvgShapes.StepBadgeSvg(v.N, step, v.Style.Stroke);

    static string EdgeLabelTransform(EdgeGeom geo) =>
        $"translate({F(geo.LabelX)},{F(geo.LabelY)}) rotate({F(geo.LabelAngle)})";

    /// <summary>A node's label lines with the tspan line offset (first line 0, then one line height each).</summary>
    static IEnumerable<(string Text, string Dy)> LabelLines(LabelLayout lay) =>
        lay.Lines.Select((line, i) => (line, i == 0 ? "0" : F(TextLayout.LineHeight)));

    /// <summary>Description lines with the tspan line offset.</summary>
    static IEnumerable<(string Text, string Dy)> DescLines(LabelLayout lay) =>
        lay.Desc.Select((line, i) => (line, i == 0 ? "0" : F(TextLayout.DescLineHeight)));


    /// <summary>Canvas background: grey around a page sheet, otherwise the chosen grid (or nothing).</summary>
    string CanvasFill =>
        D.Page.Enabled ? "#e3e7ec" : GridLook.CanvasPattern(grid) is { } pattern ? $"url(#{pattern})" : "transparent";

    /// <summary>The grid drawn on the page sheet, or null for no grid.</summary>
    string? PageGridFill => GridLook.PagePattern(grid) is { } pattern ? $"url(#{pattern})" : null;


    string GridTransform => $"translate({F(PanX)},{F(PanY)}) scale({F(Zoom)})";

    MarkupString ArrowMarkerDefs => (MarkupString)string.Concat(D.Edges.Append(D.DefaultLook)
        .Select(x => (Colour: LineLook.Colour(x.Color), x.Weight)).Distinct()
        .Select(m => LineLook.MarkerDef(m.Colour, m.Weight)));

    /// <summary>Groups in drawing order: a group inside another is drawn over it.</summary>
    IEnumerable<GroupView> GroupViews() =>
        Editor.Tree.PaintOrder().Select(g =>
        {
            (double ax, double ay, double aw, double ah) = g.TitleArea();
            (double tx, double ty, string anchor) = g.TitlePos();
            double width = GroupLook.Width(g.BorderWeight) + (Editor.Selection.Contains(g.Id) ? 1 : 0);
            return new GroupView(g, Palette.Get(g.Color), Editor.Selection.Contains(g.Id), g.BandPath(), new Rect(ax, ay, aw, ah),
                                 tx, ty, anchor, g.FillOpacity(), LineLook.Dash(g.Border, width), width);
        });

    /// <summary>Selection outline drawn just outside a group (and around a title shown under it).</summary>
    static Rect GroupSelectionRect(Group g) =>
        new Rect(g.X - 5, g.Y - 5, g.W + 10, g.H + 10 + TextLayout.ExtraBelow(g));

    /// <summary>Inline title editor: inside the title area, at least 120px wide.</summary>
    static Rect GroupTitleEditRect(GroupView v) =>
        new Rect(v.TitleArea.X + 8, v.TitleArea.Y + (v.TitleArea.H - 24) / 2, Math.Max(120, v.TitleArea.W - 16), 24);

    IEnumerable<TextBoxView> TextBoxViews() =>
        D.TextBoxes.Select(t =>
        {
            TextStyle ts = TextStyle.For(t);
            return new TextBoxView(t, ts, Editor.Selection.Contains(t.Id), TextLayout.Layout(t), SvgShapes.TextBoxPath(t, ts.Radius));
        });

    /// <summary>Connectors to draw: the ones under the shapes, or (<paramref name="rowLayer"/>) the ones that end on a
    /// row of a list / IPO node, drawn above the shapes so that end isn't hidden.</summary>
    IEnumerable<EdgeView> EdgeViews(bool rowLayer = false)
    {
        foreach (Edge e in D.Edges.Where(e => e.EndsOnRow == rowLayer))
        {
            if (Editor.Ends(e) is not (IBox a, IBox b)) continue;
            double lw = LineLook.Width(e.Weight);
            string lc = LineLook.Colour(e.Color);
            yield return new EdgeView(e, Editor.SelectedEdgeId == e.Id, EdgeRouter.EdgeGeometry(a, b, e), lw, lc, $"url(#{LineLook.MarkerId(lc, e.Weight)})");
        }
    }

    /// <summary>Connector labels that need drawing (or editing), in their own layer above the shapes.</summary>
    IEnumerable<EdgeLabelView> EdgeLabelViews()
    {
        foreach (Edge e in D.Edges)
        {
            bool editing = Editor.EditingId == e.Id;
            if (string.IsNullOrWhiteSpace(e.Label) && !editing) continue;
            if (Editor.Ends(e) is not (IBox a, IBox b)) continue;
            EdgeGeom geo = EdgeRouter.EdgeGeometry(a, b, e);
            yield return new EdgeLabelView(e, Editor.SelectedEdgeId == e.Id, editing, geo, EdgeLabels.LayoutEdgeLabel(e, geo));
        }
    }

    IEnumerable<NodeView> NodeViews() =>
        D.Nodes.Select(n =>
        {
            LabelLayout lay = TextLayout.Layout(n);
            Rect? back = TextLayout.DrawnTextBack(n, lay) is { } r ? new Rect(r.X, r.Y, r.W, r.H) : null;
            return new NodeView(n, NodeTypes.Get(n.Type), n.Kind.OutlineOf(n), NodeStyle.For(n), Editor.Selection.Contains(n.Id), lay, TextLayout.ExtraBelow(n), back);
        });

    /// <summary>Where the inline label editor sits: under the shape for labels drawn below, otherwise inside it.</summary>
    static Rect NodeEditRect(NodeView v) =>
        v.N.Kind.LabelEditRect(v.N) is { } own ? own
        : v.Layout.Below
        ? new Rect(v.N.X + v.N.W / 2 - 80, v.N.Y + v.N.H + 4, 160, 44)
        : new Rect(v.N.X + 8, v.N.Y + 6, v.N.W - 16, v.N.H - 12);

    static bool HasProperties(Node n) => n.Properties.Values.Any(v => !string.IsNullOrWhiteSpace(v));

    /// <summary>The live connector drawn while dragging from a port (null when not connecting).</summary>
    ConnectPreview? LiveConnector()
    {
        if (gesture is not ConnectGesture cg || cg.From is not { } from) return null;
        IBox? target = cg.Target;
        (double dropX, double dropY) = cg.DropPoint;
        IBox to = target ?? new TextBox { X = dropX, Y = dropY, W = 0, H = 0 };
        return new ConnectPreview(EdgeRouter.EdgeGeometry(from, to, D.DefaultConnector).D, target);
    }

    /// <summary>Handles on the selected connector: ends (move to another side) and the bend (drag sideways).</summary>
    EdgeHandleView? SelectedEdgeHandles()
    {
        if (Editor.SelectedEdge is not { } he || Editor.Ends(he) is not (IBox ha, IBox hb) || Editor.EditingId == he.Id) return null;
        EdgeRoute hr = EdgeRouter.Route(ha, hb, he);
        List<SideTarget> sides = new();
        if (gesture is EdgeEndGesture eg)
        {
            IBox box = eg.FromEnd ? ha : hb;
            Side? current = eg.FromEnd ? hr.FromSide : hr.ToSide;
            foreach (Side side in box.ConnectSides)
            {
                (double sx, double sy) = EdgeRouter.SidePoint(box, side);
                sides.Add(new SideTarget(sx, sy, side == current));
            }
        }
        (double bx, double by) = hr.BendHandle;
        (double p0x, double p0y) = hr.Points[0];
        (double p1x, double p1y) = hr.Points[^1];
        return new EdgeHandleView(he.Id, sides, hr.BendIndex >= 0 && he.Style != EdgeStyle.Straight, bx, by, hr.BendIsVertical, p0x, p0y, p1x, p1y);
    }

    int OffPageCount => Editor.OutsidePage().Count;

    string? LinkExclude => SavedName;

    /// <summary>Inline editor styled like the box itself, so editing looks the same as the finished text.</summary>
    static string TextEditStyle(TextBox t, TextStyle s) =>
        $"font-family:{s.Family};font-size:{F(t.FontSize)}px;line-height:{F(t.FontSize * TextLook.LineSpacing)}px;" +
        $"font-weight:{(t.Bold ? "600" : "400")};color:{s.TextColour};text-align:{t.Align};padding:{F(TextLook.PadY)}px {F(TextLook.PadX)}px;";

    // Small UI handlers that used to be inline lambdas in the markup.

    /// <summary>After the lane creator adds lanes: fit them in view.</summary>
    Task FitAfterLanes() => Run(Cmd.Fit);

    void CloseMenus() => openMenu = null;
    void DiscardAndNew() { Reset(); CloseDialog(); }

}
