using BeDiagram.Components.Pages;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Bars;

/// <summary>The bar along the top: the menu bar (File, Edit, Arrange, View, Help), the linked-diagram breadcrumb, undo / redo, grid, zoom, flow direction and snap, and the diagram name.</summary>
public partial class Toolbar : DesignerPart
{
    void CloseMenus()
    {
        Host.SetOpenMenu(null);
    }

    string SnapClass
    {
        get
        {
            if (Editor.Snap)
            {
                return "on";
            }
            return "";
        }
    }

    string SnapTip
    {
        get
        {
            if (Editor.Snap)
            {
                return "Snap to grid is on (10 px) — click to turn off";
            }
            return "Snap to grid is off — click to turn on";
        }
    }

    void ToggleSnap()
    {
        Editor.Snap = !Editor.Snap;
    }

    string FlowClass(FlowDirection flow)
    {
        if (D.Flow == flow)
        {
            return "on";
        }
        return "";
    }

    List<Crumb> Crumbs
    {
        get
        {
            List<Crumb> result = new List<Crumb>();
            for (int i = 0; i < Host.Trail.Count; i++)
            {
                result.Add(new Crumb(i, Host.Trail[i]));
            }
            return result;
        }
    }
}
