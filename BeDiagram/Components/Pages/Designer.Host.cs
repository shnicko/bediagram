using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Components.Pages.Commands;
using BeDiagram.Components.Pages.Gestures;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Editing;
using BeDiagram.Editing.Lanes;
using BeDiagram.Services.Export;

namespace BeDiagram.Components.Pages;

/// <summary>
/// The Designer as the host of its child components (<see cref="IDesignerHost"/>): the bars, side panels and canvas
/// layers reach the editor, the command buttons and the page's own actions through this, and the layers hand their
/// pointer presses back (<see cref="ICanvasInput"/>).
/// </summary>
public partial class Designer : IDesignerHost, ICanvasInput
{
    DiagramEditor IDesignerHost.Editor
    {
        get
        {
            return Editor;
        }
    }

    CommandButtons IDesignerHost.Buttons
    {
        get
        {
            return Cmd;
        }
    }

    void IDesignerHost.Redraw()
    {
        StateHasChanged();
    }

    void IDesignerHost.ShowStepOrder()
    {
        ShowStepOrder();
    }

    void IDesignerHost.ShowShortcuts()
    {
        ShowShortcuts();
    }

    void IDesignerHost.ShowJson()
    {
        ShowJson();
    }

    void IDesignerHost.NewDiagram()
    {
        NewDiagram();
    }

    void IDesignerHost.ShowOpen()
    {
        ShowOpen();
    }

    Task IDesignerHost.Export(ExportChoice choice, ExportOptions options)
    {
        return Export(choice, options);
    }

    bool IDesignerHost.Painting
    {
        get
        {
            return painting;
        }
    }

    // Tabs

    IReadOnlyList<DiagramTab> IDesignerHost.Tabs
    {
        get
        {
            return tabs;
        }
    }

    DiagramTab IDesignerHost.ActiveTab
    {
        get
        {
            return active;
        }
    }

    void IDesignerHost.SwitchTab(DiagramTab tab)
    {
        SwitchTo(tab);
    }

    void IDesignerHost.CloseTab(DiagramTab tab)
    {
        CloseTab(tab);
    }

    string? IDesignerHost.WindowLink(DiagramTab tab)
    {
        return WindowLink(tab);
    }

    void IDesignerHost.MovedToWindow(DiagramTab tab)
    {
        MovedToWindow(tab);
    }

    // Linked diagrams

    void IDesignerHost.FollowLink(string? link)
    {
        FollowLink(link);
    }

    Task IDesignerHost.FollowLinkInNewTab(string? link)
    {
        return FollowLinkInNewTab(link);
    }

    string? IDesignerHost.LinkExclude
    {
        get
        {
            return LinkExclude;
        }
    }

    IReadOnlyList<string> IDesignerHost.Trail
    {
        get
        {
            return trail;
        }
    }

    void IDesignerHost.GoBack()
    {
        GoBack();
    }

    void IDesignerHost.GoUpTo(int level)
    {
        GoUpTo(level);
    }

    // Page and view

    void IDesignerHost.SetPageSize(string? size)
    {
        SetPageSize(size);
    }

    void IDesignerHost.SetLandscape(bool landscape)
    {
        if (landscape)
        {
            SetOrientation("landscape");
        }
        else
        {
            SetOrientation("portrait");
        }
    }

    double IDesignerHost.Zoom
    {
        get
        {
            return Zoom;
        }
    }

    string IDesignerHost.Grid
    {
        get
        {
            return grid;
        }
    }

    void IDesignerHost.SetGrid(string key)
    {
        grid = key;
    }

    string? IDesignerHost.OpenMenu
    {
        get
        {
            return openMenu;
        }
    }

    void IDesignerHost.SetOpenMenu(string? menu)
    {
        openMenu = menu;
    }

    string? IDesignerHost.Toast
    {
        get
        {
            return toast;
        }
    }

    void IDesignerHost.SetPaletteDrop(Action<double, double> drop)
    {
        paletteDrop = drop;
    }

    // Canvas input

    ICanvasInput IDesignerHost.Canvas
    {
        get
        {
            return this;
        }
    }

    void ICanvasInput.ItemDown(PointerEventArgs e, string id)
    {
        ItemDown(e, id);
    }

    void ICanvasInput.GroupBodyDown(PointerEventArgs e, string id)
    {
        GroupBodyDown(e, id);
    }

    void ICanvasInput.PortDown(PointerEventArgs e, string id, string? itemId)
    {
        PortDown(e, id, itemId);
    }

    void ICanvasInput.DividerDown(PointerEventArgs e, LaneDivider d)
    {
        DividerDown(e, d);
    }

    void ICanvasInput.ResizeDown(PointerEventArgs e, string id, ResizeHandle handle)
    {
        ResizeDown(e, id, handle);
    }

    void ICanvasInput.OutsideTextDown(PointerEventArgs e, string id)
    {
        OutsideTextDown(e, id);
    }

    void ICanvasInput.EdgeDown(PointerEventArgs e, string id)
    {
        EdgeDown(e, id);
    }

    void ICanvasInput.HeadSpotDown(PointerEventArgs e, string id, bool atStart)
    {
        HeadSpotDown(e, id, atStart);
    }

    HeadPick? IDesignerHost.HeadPick
    {
        get
        {
            return headPick;
        }
    }

    void IDesignerHost.CloseHeadPicker()
    {
        headPick = null;
    }

    void ICanvasInput.RowEditorDown(PointerEventArgs e, string id)
    {
        RowEditorDown(e, id);
    }

    bool IDesignerHost.RowEditorOpen
    {
        get
        {
            return rowEditorOpen;
        }
    }

    double IDesignerHost.RowEditorLeft
    {
        get
        {
            return rowEditorShownLeft;
        }
    }

    double IDesignerHost.RowEditorTop
    {
        get
        {
            return rowEditorShownTop;
        }
    }

    void IDesignerHost.OpenRowEditor()
    {
        OpenRowEditor();
    }

    void IDesignerHost.CloseRowEditor()
    {
        CloseRowEditor();
    }
}
