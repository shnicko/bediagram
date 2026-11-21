using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BeDiagram.Components.Pages;

/// <summary>The row editor window (<see cref="Panels.RowEditorWindow"/>): a floating window over the page for the rows
/// of the selected list, IPO box, class or entity. The page keeps whether it's open and where it is, so it stays put
/// while the selection changes. The browser moves it while it's dragged by its title (designer.js) and reports
/// where it was dropped (<see cref="OnWindowMoved"/>).</summary>
public partial class Designer
{
    /// <summary>Width of the window when it first opens (it can be resized by its corner).</summary>
    public const double RowEditorWidth = 600;

    bool rowEditorOpen;

    /// <summary>Screen position of the window's top-left corner, as last dropped; null until it first opens.</summary>
    double? rowEditorLeft;
    double? rowEditorTop;

    /// <summary>Where the window is drawn while open: fixed when it opens, so redraws never rewrite its style and
    /// undo the browser's own moves and corner resizes (Blazor only touches attributes whose value changed).</summary>
    double rowEditorShownLeft;
    double rowEditorShownTop;

    /// <summary>Opens the window; the first time, in the top-right corner of the canvas.</summary>
    void OpenRowEditor()
    {
        if (rowEditorLeft is null || rowEditorTop is null)
        {
            rowEditorLeft = Math.Max(canvasLeft + 16, canvasLeft + canvasW - RowEditorWidth - 16);
            rowEditorTop = canvasTop + 16;
        }
        if (!rowEditorOpen)
        {
            rowEditorShownLeft = rowEditorLeft.Value;
            rowEditorShownTop = rowEditorTop.Value;
        }
        rowEditorOpen = true;
    }

    void CloseRowEditor()
    {
        rowEditorOpen = false;
    }

    /// <summary>The window's ⤢ on a list, IPO box, class or entity: select that node and open the window.</summary>
    void RowEditorDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0)
        {
            StartPan(e);
            return;
        }
        headPick = null;
        Editor.EndEdit();
        Editor.SelectOnly(id);
        OpenRowEditor();
    }

    /// <summary>The browser moved the window (dragged by its title): remember where, so a redraw keeps it there.</summary>
    [JSInvokable]
    public void OnWindowMoved(double left, double top)
    {
        rowEditorLeft = left;
        rowEditorTop = top;
    }
}
