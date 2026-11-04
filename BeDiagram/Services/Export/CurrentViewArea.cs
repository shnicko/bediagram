using BeDiagram.Models;

namespace BeDiagram.Services.Export;

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
