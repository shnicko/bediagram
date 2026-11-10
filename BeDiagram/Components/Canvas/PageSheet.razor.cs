using BeDiagram.Models.Styles;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Components.Canvas;

/// <summary>The paper sheet under the drawing when a page size is set: white sheet with shadow, the grid on it, the margin and a caption.</summary>
public partial class PageSheet : CanvasLayer
{

    double PrintableW => Math.Max(0, D.Page.WidthPx - 2 * D.Page.MarginPx);

    double PrintableH => Math.Max(0, D.Page.HeightPx - 2 * D.Page.MarginPx);

    string PageCaptionStyle => $"font-size:{F(12 / Host.Zoom)}px";

    /// <summary>The grid drawn on the page sheet, or null for no grid.</summary>
    string? PageGridFill => GridLook.PagePattern(Host.Grid) is { } pattern ? $"url(#{pattern})" : null;
}
