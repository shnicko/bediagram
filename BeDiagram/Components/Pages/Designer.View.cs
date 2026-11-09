using BeDiagram.Models;
using Microsoft.JSInterop;
using BeDiagram.Models.Text;

namespace BeDiagram.Components.Pages;

/// <summary>Pan and zoom: fit, zoom to selection, wheel zoom, centring on an item.</summary>
public partial class Designer
{
    // ───────────── View ─────────────

    void ZoomAt(double factor, double px, double py)
    {
        double z = Math.Clamp(Zoom * factor, 0.05, 3);
        PanX = px - (px - PanX) * z / Zoom;
        PanY = py - (py - PanY) * z / Zoom;
        Zoom = z;
    }

    void ZoomBy(double factor) => ZoomAt(factor, canvasW / 2, canvasH / 2);

    void Fit()
    {
        double minX, minY, maxX, maxY;
        (double X, double Y, double W, double H)? cb = Boxes.ContentBounds(D);
        if (D.Page.Enabled)
        {
            // Show the whole page (plus anything hanging off it).
            minX = 0; minY = 0; maxX = D.Page.WidthPx; maxY = D.Page.HeightPx;
            if (cb is { } c) { minX = Math.Min(minX, c.X); minY = Math.Min(minY, c.Y); maxX = Math.Max(maxX, c.X + c.W); maxY = Math.Max(maxY, c.Y + c.H); }
            minY -= 24; // room for the page caption
        }
        else
        {
            if (cb is not { } c) { Zoom = 1; PanX = 40; PanY = 40; return; }
            (minX, minY, maxX, maxY) = (c.X, c.Y, c.X + c.W, c.Y + c.H);
        }
        ZoomToBounds(minX, minY, maxX, maxY);
    }

    /// <summary>Zooms and centres on the selected items, labels under shapes included. A selected connector counts as its two ends.</summary>
    void FitSelection()
    {
        List<IBox> boxes = Editor.Selection.Select(Editor.FindBox).OfType<IBox>().ToList();
        if (Editor.SelectedEdge is { } e) boxes.AddRange(new[] { Editor.FindBox(e.From), Editor.FindBox(e.To) }.OfType<IBox>());
        if (boxes.Count == 0) { ShowToast("Select something to zoom to"); return; }
        ZoomToBounds(boxes.Min(b => b.X), boxes.Min(b => b.Y),
                     boxes.Max(b => b.X + b.W), boxes.Max(b => b.Y + b.H + TextLayout.ExtraBelow(b)));
    }

    /// <summary>Zooms so the area fits the canvas with a 40px margin (at most 150%), centred.</summary>
    void ZoomToBounds(double minX, double minY, double maxX, double maxY)
    {
        Zoom = Math.Clamp(Math.Min((canvasW - 80) / Math.Max(1, maxX - minX), (canvasH - 80) / Math.Max(1, maxY - minY)), 0.05, 1.5);
        PanX = canvasW / 2 - (minX + maxX) / 2 * Zoom;
        PanY = canvasH / 2 - (minY + maxY) / 2 * Zoom;
    }

    [JSInvokable]
    public void OnCanvasRect(double left, double top, double width, double height)
    {
        canvasLeft = left; canvasTop = top; canvasW = width; canvasH = height;
    }

    [JSInvokable]
    public void OnWheel(double dx, double dy, bool ctrl, bool shift, double px, double py)
    {
        if (ctrl) ZoomAt(Math.Exp(-dy * 0.0015), px, py);
        else if (shift) PanX -= dy;
        else { PanX -= dx; PanY -= dy; }
        StateHasChanged();
    }
}
