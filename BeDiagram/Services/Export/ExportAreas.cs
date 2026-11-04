using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>The whole sheet when a page size is set, otherwise the drawing zoomed to its extents.</summary>
public class WholeDrawingArea : IExportArea
{
    public string FileSuffix => "";

    public string Label(Diagram d) => d.Page.Enabled
        ? $"page ({d.Page.Mm.W:0.#} × {d.Page.Mm.H:0.#} mm)"
        : "drawing extents";

    public string Tip(Diagram d) => d.Page.Enabled
        ? "The whole page at the selected paper size"
        : "The whole drawing, zoomed to its extents";

    public ExportView? ViewFor(Diagram d, ScreenView screen) => null;
}

/// <summary>What is on screen now — fitted inside the page margins when a page size is set.</summary>
public class CurrentViewArea : IExportArea
{
    public string FileSuffix => ".view";

    public string Label(Diagram d) => "current view";

    public string Tip(Diagram d) => d.Page.Enabled
        ? "What is on screen now, fitted inside the page margins"
        : "Exactly what is on screen now";

    public ExportView? ViewFor(Diagram d, ScreenView s) =>
        new ExportView(-s.PanX / s.Zoom, -s.PanY / s.Zoom, s.CanvasW / s.Zoom, s.CanvasH / s.Zoom, s.CanvasW, s.CanvasH);
}
