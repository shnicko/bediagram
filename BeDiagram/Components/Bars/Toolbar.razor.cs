using BeDiagram.Components.Pages;
using BeDiagram.Components.Pages.Commands;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Editing.Angles;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Bars;

/// <summary>The bar along the top: the menu bar (File, Edit, Arrange, View, Help), the linked-diagram breadcrumb, undo / redo, the format painter, grid, zoom, flow direction, snap and angle lock, and the diagram name.</summary>
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

    /// <summary>The format painter brush: lit while it is on.</summary>
    string PainterClass
    {
        get
        {
            if (Host.Painting)
            {
                return "on";
            }
            return "";
        }
    }

    static string PainterTip
    {
        get
        {
            return "Format painter (P): copies the look of the selected item, then click an item to paint it on. "
                + "Double-click (Shift+P) to keep painting until Esc. Edit menu: Copy style / Paste style / Paste special.";
        }
    }

    Task Paint()
    {
        return Run(Cmd.Painter);
    }

    Task PaintSticky()
    {
        return Run(Cmd.PainterSticky);
    }

    void ToggleSnap()
    {
        Editor.Snap = !Editor.Snap;
    }

    string AngleClass(IAngleLock a)
    {
        if (Editor.AngleLock == a)
        {
            return "on";
        }
        return "";
    }

    /// <summary>Tooltip for the angle lock buttons, naming the toggle key.</summary>
    string AngleTip
    {
        get
        {
            return "Angle lock when moving items and placing joined steps — "
                + CommandButtons.KeyText(Cmd.AngleToggle) + " switches between Free and the last lock";
        }
    }

    /// <summary>Same setting as the Edit menu's angle lock: kept while the page is open, not saved in the file.</summary>
    void SetAngleLock(IAngleLock a)
    {
        Editor.AngleLock = a;
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
