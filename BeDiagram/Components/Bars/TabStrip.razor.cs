using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Components.Pages;

namespace BeDiagram.Components.Bars;

/// <summary>
/// The open diagrams, one tab each, above the canvas (shown once there is more than one). Click shows a tab,
/// × or middle-click closes it, right-click opens its menu (move to a new window, close), + starts a new diagram.
/// </summary>
public partial class TabStrip : DesignerPart
{
    /// <summary>The page's open-menu name for a tab's right-click menu.</summary>
    static string MenuName(DiagramTab t)
    {
        return "tab:" + t.Id;
    }

    bool MenuOpenFor(DiagramTab t)
    {
        return Host.OpenMenu == MenuName(t);
    }

    string TabClass(DiagramTab t)
    {
        if (t == Host.ActiveTab)
        {
            return "tab on";
        }
        return "tab";
    }

    /// <summary>The full name (with its folder) and whether it is saved.</summary>
    static string TabTip(DiagramTab t)
    {
        string name = t.Editor.Diagram.Name;
        if (!t.HasFile)
        {
            return name + " (not saved yet)";
        }
        if (t.Editor.Dirty)
        {
            return name + " (unsaved changes)";
        }
        return name;
    }

    static string CloseTip
    {
        get
        {
            return "Close tab (Alt+W)";
        }
    }

    /// <summary>Why a tab can't move to a new window: the new window opens the saved file.</summary>
    static string MoveTip(DiagramTab t)
    {
        if (!t.HasFile)
        {
            return "Save the diagram first: the new window opens the saved file";
        }
        return "Save your changes first: the new window opens the saved file";
    }

    void Show(DiagramTab t)
    {
        Host.SwitchTab(t);
    }

    void Close(DiagramTab t)
    {
        Host.SetOpenMenu(null);
        Host.CloseTab(t);
    }

    /// <summary>The middle button closes a tab, as in a web browser.</summary>
    void MouseDown(MouseEventArgs e, DiagramTab t)
    {
        if (e.Button == 1)
        {
            Close(t);
        }
    }

    void OpenTabMenu(DiagramTab t)
    {
        Host.SetOpenMenu(MenuName(t));
    }

    void Moved(DiagramTab t)
    {
        Host.MovedToWindow(t);
    }

    void New()
    {
        Host.NewDiagram();
    }
}
