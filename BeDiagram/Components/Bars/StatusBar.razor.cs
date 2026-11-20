using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Services;

namespace BeDiagram.Components.Bars;

/// <summary>The bar along the bottom: storage folder, page size, items off the page, the shape lock, the copied style
/// and the latest message.</summary>
public partial class StatusBar : DesignerPart
{
    [Inject] DiagramStore Store { get; set; } = default!;
    [Inject] StyleClipboard Clipboard { get; set; } = default!;

    void ClearStyle()
    {
        Clipboard.Clear();
    }

    int OffPageCount
    {
        get
        {
            return Editor.OutsidePage().Count;
        }
    }

    /// <summary>The Unlock link next to "Shapes locked in place".</summary>
    void Unlock()
    {
        Editor.SetShapesLocked(false);
    }
}
