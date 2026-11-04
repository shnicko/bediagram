using BeDiagram.Models;

namespace BeDiagram.Services.Export;

/// <summary>
/// Strategy for which part of the drawing goes into an export: the whole page / drawing extents,
/// or what is on screen now. Works with any <see cref="IExportFormat"/> whose <see cref="IExportFormat.UsesArea"/> is true.
/// </summary>
public interface IExportArea
{
    /// <summary>Added before the file extension so areas don't overwrite each other, e.g. ".view" → "Name.view.png".</summary>
    string FileSuffix { get; }

    /// <summary>Menu text after the format name, e.g. "page (297 × 210 mm)" or "current view".</summary>
    string Label(Diagram d);

    /// <summary>Tooltip for the menu entry.</summary>
    string Tip(Diagram d);

    /// <summary>The window to export, in world coordinates. Null means the whole page (or drawing extents).</summary>
    ExportView? ViewFor(Diagram d, ScreenView screen);
}
