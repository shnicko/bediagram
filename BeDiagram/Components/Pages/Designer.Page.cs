using BeDiagram.Models;

namespace BeDiagram.Components.Pages;

/// <summary>Paper size and orientation (from the File menu and the Diagram panel), and the page sheet drawn on the canvas.</summary>
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

    // ───────────── The page sheet on the canvas ─────────────


}
