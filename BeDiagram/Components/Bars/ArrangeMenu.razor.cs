using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;

namespace BeDiagram.Components.Bars;

/// <summary>The Arrange menu: step order, tidy connectors, auto-layout, swim lanes, lane lock, shape lock and the style
/// for new connectors.</summary>
public partial class ArrangeMenu : DesignerPart
{

    void SetLockLanes(ChangeEventArgs e)
    {
        Editor.SetLockLanes(e.Value is true);
    }

    void SetShapesLocked(ChangeEventArgs e)
    {
        Editor.SetShapesLocked(e.Value is true);
    }
}
