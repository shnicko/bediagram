using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages.Gestures;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Routing;
using BeDiagram.Models.Styles;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Components.Pages;

/// <summary>Values the page's own markup reads: the canvas background, grid and arrowhead markers, and the gesture
/// previews (live connector, the selected connector's handles). The canvas layers (Components/Canvas) work out their own.</summary>
public partial class Designer
{
    // ───────────── Render helpers (read by Designer.razor) ─────────────
    // The markup only loops and reads values; anything that needs working out lives here.



    double BendHandleSize => 12 / Zoom;


    /// <summary>Canvas background: grey around a page sheet, otherwise the chosen grid (or nothing).</summary>
    string CanvasFill =>
        D.Page.Enabled ? "#e3e7ec" : GridLook.CanvasPattern(grid) is { } pattern ? $"url(#{pattern})" : "transparent";


    string GridTransform => $"translate({F(PanX)},{F(PanY)}) scale({F(Zoom)})";

    MarkupString ArrowMarkerDefs => (MarkupString)string.Concat(D.Edges.Append(D.DefaultLook)
        .Select(x => (Colour: LineLook.Colour(x.Color), x.Weight)).Distinct()
        .Select(m => LineLook.MarkerDef(m.Colour, m.Weight)));

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


    string? LinkExclude => SavedName;

    // Small UI handlers that used to be inline lambdas in the markup.

    /// <summary>After the lane creator adds lanes: fit them in view.</summary>
    Task FitAfterLanes() => Run(Cmd.Fit);

    void CloseMenus() => openMenu = null;
    void DiscardAndNew() { Reset(); CloseDialog(); }

}
