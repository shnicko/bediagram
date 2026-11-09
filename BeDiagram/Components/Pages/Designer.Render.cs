using BeDiagram.Models.Serialization;
using BeDiagram.Models;
using BeDiagram.Components.Pages.Gestures;
using BeDiagram.Services;
using BeDiagram.Services.Export;
using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Editing.Angles;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Text;
using BeDiagram.Models.Routing;
using BeDiagram.Rendering;

namespace BeDiagram.Components.Pages;

/// <summary>Values the markup reads: view records for nodes, connectors, groups and text boxes, palette tiles, tooltips and panel text.</summary>
public partial class Designer
{
    // ───────────── Render helpers (read by Designer.razor) ─────────────
    // The markup only loops and reads values; anything that needs working out lives here.

    static readonly string[] EdgeLabelChips = { "Yes", "No", "Pass", "Fail", "" };

    /// <summary>Palette shapes (outcomes have their own section), numbered for the 1–9 shortcut keys.</summary>
    static readonly List<PaletteShape> PaletteShapes = NodeTypes.All
        .Where(t => !t.Kind.HasOutcome)
        .Select((t, i) => new PaletteShape(t, i + 1))
        .ToList();

    /// <summary>The classic shapes, each offered again as a circle with its type's icon (<see cref="Node.Round"/>).</summary>
    static readonly List<NodeTypeInfo> RoundShapes = NodeTypes.All.Where(t => t.Kind.CanBeRound).ToList();

    /// <summary>The Type dropdown: options carry the saved type names ("decision").</summary>
    void TypePicked(Node n, ChangeEventArgs e) => Editor.ChangeType(n, EnumNames.Parse<NodeType>(e.Value?.ToString()));

    /// <summary>The inside of a list / IPO node (title band, sections, rows) — same markup as the export — or null.</summary>
    static MarkupString? PanelBody(NodeView v) => v.N.Kind.Body(v.N, v.Style) is { } body ? (MarkupString)body : null;

    void SetPortrait() => SetOrientation("portrait");
    void SetLandscape() => SetOrientation("landscape");

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

    /// <summary>What the text width applies to for this shape, for the panel hint.</summary>
    static string TextWidthHint(Node n) =>
        n.Kind.LabelBelow(n) ? "label and description under the shape" : "description under the shape";

    static string TextWidthPlaceholder(Node n) => $"Auto ({F(TextLayout.DefaultTextWidth(n))})";

    static string NodePanelTitle(Node n) => n.Kind.PanelTitle(n);


    string DiagramSummary =>
        $"{D.Nodes.Count} shapes · {D.Edges.Count} connectors · {D.TextBoxes.Count(t => !t.IsComment)} text boxes · " +
        $"{D.TextBoxes.Count(t => t.IsComment)} comments · {D.Groups.Count} groups";

    IEnumerable<Crumb> Crumbs => trail.Select((name, i) => new Crumb(i, name));

    /// <summary>Canvas background: grey around a page sheet, otherwise the chosen grid (or nothing).</summary>
    string CanvasFill =>
        D.Page.Enabled ? "#e3e7ec" : GridLook.CanvasPattern(grid) is { } pattern ? $"url(#{pattern})" : "transparent";

    /// <summary>The grid drawn on the page sheet, or null for no grid.</summary>
    string? PageGridFill => GridLook.PagePattern(grid) is { } pattern ? $"url(#{pattern})" : null;

    void SetGrid(string key) => grid = key;

    /// <summary>Toolbar angle lock: a view-time setting on the editor, kept while the page is open, not saved in the file.</summary>
    void SetAngleLock(IAngleLock a) => Editor.AngleLock = a;

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

    /// <summary>Step number typed in the panel: a whole number makes the shape that step (others move down one).</summary>
    void StepTyped(Node n, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out int step)) Editor.SetStep(n, step);
    }

    /// <summary>A shape can run in parallel when there's another step to share a number with (or it already does).</summary>
    bool CanBeParallel(Node n) => Editor.Steps.IsParallel(n) || Editor.Steps.Last > 1;

    /// <summary>"Shares step 4 with Design Review" under the step box, or null when the shape has a number of its own.</summary>
    string? ParallelNote(Node n)
    {
        List<Node> others = Editor.Steps.ParallelWith(n);
        if (others.Count == 0) return null;
        return $"Shares step {n.Step} with {string.Join(", ", others.Select(o => string.IsNullOrWhiteSpace(o.Label) ? NodeTypes.Get(o.Type).Display : o.Label.Replace("\n", " ")))}";
    }

    /// <summary>How many of the selected items are steps (shapes other than outcomes).</summary>
    int SelectedStepCount => D.Nodes.Count(n => Editor.Selection.Contains(n.Id) && n.Kind.IsStep);

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

    /// <summary>Icon picker contents, filtered by the search box (empty categories dropped).</summary>
    List<(string Category, List<string> Names)> FilteredIcons() =>
        Icons.Categories
            .Select(c => (c.Category, Names: c.Names.Where(nm => string.IsNullOrWhiteSpace(iconFilter) || nm.Contains(iconFilter.Trim(), StringComparison.OrdinalIgnoreCase)).ToList()))
            .Where(c => c.Names.Count > 0)
            .ToList();

    int MemberCount(Group g) => Editor.Members(g).Count();

    int OffPageCount => Editor.OutsidePage().Count;

    string? LinkExclude => hasFile ? D.Name : null;

    /// <summary>Inline editor styled like the box itself, so editing looks the same as the finished text.</summary>
    static string TextEditStyle(TextBox t, TextStyle s) =>
        $"font-family:{s.Family};font-size:{F(t.FontSize)}px;line-height:{F(t.FontSize * TextLook.LineSpacing)}px;" +
        $"font-weight:{(t.Bold ? "600" : "400")};color:{s.TextColour};text-align:{t.Align};padding:{F(TextLook.PadY)}px {F(TextLook.PadX)}px;";

    // Small UI handlers that used to be inline lambdas in the markup.

    /// <summary>After the lane creator adds lanes: fit them in view.</summary>
    Task FitAfterLanes() => Run(FitCmd);

    void SetOpenMenu(string? menu) => openMenu = menu;
    void CloseMenus() => openMenu = null;
    void AskDelete(string name) => confirmDelete = name;
    void DiscardAndNew() { Reset(); CloseDialog(); }

    void SetPropValue(Node n, string key, ChangeEventArgs e)
    {
        n.Properties[key] = e.Value?.ToString() ?? string.Empty;
        Editor.MarkDirty();
    }

    void SetFitOnExport(ChangeEventArgs e) { Editor.Change(); D.Page.FitOnExport = e.Value is true; }
}
