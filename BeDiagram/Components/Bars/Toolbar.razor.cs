using BeDiagram.Components.Pages;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Bars;

/// <summary>The bar along the top: the menu bar (File, Edit, Arrange, View, Help), the linked-diagram breadcrumb, undo / redo, grid, zoom, flow direction and snap, and the diagram name.</summary>
public partial class Toolbar : DesignerPart
{
    void CloseMenus() => Host.SetOpenMenu(null);


    string SnapClass => Editor.Snap ? "on" : "";

    string SnapTip => Editor.Snap ? "Snap to grid is on (10 px) — click to turn off" : "Snap to grid is off — click to turn on";

    void ToggleSnap() => Editor.Snap = !Editor.Snap;

    string FlowClass(FlowDirection flow) => D.Flow == flow ? "on" : "";

    IEnumerable<Crumb> Crumbs => Host.Trail.Select((name, i) => new Crumb(i, name));
}
