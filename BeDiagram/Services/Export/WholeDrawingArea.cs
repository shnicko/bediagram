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
