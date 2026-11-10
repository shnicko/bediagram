using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Services;

namespace BeDiagram.Components.Bars;

/// <summary>The bar along the bottom: storage folder, page size, items off the page and the latest message.</summary>
public partial class StatusBar : DesignerPart
{
    [Inject] DiagramStore Store { get; set; } = default!;

    int OffPageCount => Editor.OutsidePage().Count;
}
