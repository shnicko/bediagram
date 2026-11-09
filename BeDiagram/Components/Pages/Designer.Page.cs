using BeDiagram.Models;
using Microsoft.AspNetCore.Components;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Pages;

/// <summary>Paper size, orientation and margins, and the flow direction / lane lock menu toggles.</summary>
public partial class Designer
{
    // ───────────── Page (paper size) ─────────────

    void SetPageSize(string? size)
    {
        if (size is null || size == D.Page.Size) return;
        Editor.Change();
        bool wasOpen = !D.Page.Enabled;
        D.Page.Size = size;
        if (D.Page.Enabled && wasOpen && Editor.OutsidePage().Count > 0) Editor.CentreOnPage(checkpoint: false);
        Fit();
        if (D.Page.Enabled && Editor.ContentTooBigForPage())
            ShowToast(D.Page.FitOnExport ? "Drawing is bigger than the page — it will be scaled down to fit on export" : "Drawing is bigger than the page");
    }

    void SetOrientation(string orientation)
    {
        if (D.Page.Size == PageSetup.Custom)
        {
            // Custom sizes: orientation just swaps the numbers.
            bool isLandscape = D.Page.CustomW > D.Page.CustomH;
            if ((orientation == "landscape") == isLandscape) return;
            Editor.Change();
            (D.Page.CustomW, D.Page.CustomH) = (D.Page.CustomH, D.Page.CustomW);
        }
        else
        {
            if (D.Page.Orientation == orientation) return;
            Editor.Change();
        }
        D.Page.Orientation = orientation;
        Fit();
    }

    // ───────────── Flow direction and lane lock (menu toggles) ─────────────

    string FlowClass(FlowDirection flow) => D.Flow == flow ? "on" : "";

    void SetLockLanes(ChangeEventArgs e) => Editor.SetLockLanes(e.Value is true);

    string SnapClass => Editor.Snap ? "on" : "";

    string SnapTip => Editor.Snap ? "Snap to grid is on (10 px) — click to turn off" : "Snap to grid is off — click to turn on";

    void ToggleSnap() => Editor.Snap = !Editor.Snap;

    bool PageIsLandscape => D.Page.Size == PageSetup.Custom ? D.Page.CustomW > D.Page.CustomH : D.Page.Landscape;

        /// <summary>What above/below means for the selected label's text direction.</summary>

    void SetPageSize(ChangeEventArgs e) => SetPageSize(e.Value?.ToString());

    double PrintableW => Math.Max(0, D.Page.WidthPx - 2 * D.Page.MarginPx);

    double PrintableH => Math.Max(0, D.Page.HeightPx - 2 * D.Page.MarginPx);

    string PageCaptionStyle => $"font-size:{F(12 / Zoom)}px";
}
