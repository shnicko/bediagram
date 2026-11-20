using BeDiagram.Components.Pages.Commands;
using BeDiagram.Editing.Lanes;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Pages;

/// <summary>
/// The Designer as the receiver of editor commands (<see cref="ICommandContext"/>). Explicit implementations:
/// diagram actions forward to the <see cref="Editing.DiagramEditor"/>; saving, dialogs, tabs, zoom and cancelling a
/// drag are the page's own.
/// </summary>
public partial class Designer : ICommandContext
{
    bool ICommandContext.CanUndo
    {
        get
        {
            return Editor.CanUndo;
        }
    }

    bool ICommandContext.CanRedo
    {
        get
        {
            return Editor.CanRedo;
        }
    }

    bool ICommandContext.HasSelectedConnector
    {
        get
        {
            return Editor.SelectedEdge is not null;
        }
    }

    bool ICommandContext.HasSelection
    {
        get
        {
            return Editor.Selection.Count > 0 || Editor.SelectedEdge is not null;
        }
    }

    // Tabs

    int ICommandContext.TabCount
    {
        get
        {
            return tabs.Count;
        }
    }

    void ICommandContext.GoToTab(int index)
    {
        GoToTab(index);
    }

    void ICommandContext.StepTab(int delta)
    {
        StepTab(delta);
    }

    void ICommandContext.CloseActiveTab()
    {
        CloseTab(active);
    }

    void ICommandContext.NewDiagram()
    {
        NewDiagram();
    }

    // Copy style and the format painter

    bool ICommandContext.CanCopyStyle
    {
        get
        {
            return CanCopyStyle;
        }
    }

    bool ICommandContext.HasCopiedStyle
    {
        get
        {
            return Styles.Held is not null;
        }
    }

    bool ICommandContext.Painting
    {
        get
        {
            return painting;
        }
    }

    void ICommandContext.CopyStyle()
    {
        CopyStyle();
    }

    void ICommandContext.PasteStyle()
    {
        PasteStyle();
    }

    void ICommandContext.ShowPasteSpecial()
    {
        ShowPasteSpecial();
    }

    void ICommandContext.StartPainter(bool sticky)
    {
        StartPainter(sticky);
    }

    // Files and undo

    Task ICommandContext.SaveAsync()
    {
        return Save();
    }

    void ICommandContext.ShowSaveAs()
    {
        ShowSaveAs();
    }

    void ICommandContext.Undo()
    {
        Editor.Undo();
    }

    void ICommandContext.Redo()
    {
        Editor.Redo();
    }

    // Selection and editing

    void ICommandContext.SelectAll()
    {
        Editor.SelectAll();
    }

    void ICommandContext.CancelAndClearSelection()
    {
        StopPainter();
        Editor.EditingId = null;
        CancelGesture();
        Editor.ClearSelection();
    }

    void ICommandContext.EditSelected()
    {
        if (Editor.Selection.Count == 1)
        {
            Editor.BeginEdit(Editor.Selection[0]);
        }
        else if (Editor.SelectedEdgeId is not null)
        {
            Editor.BeginEdit(Editor.SelectedEdgeId);
        }
    }

    bool ICommandContext.HasShapes
    {
        get
        {
            return Editor.HasShapes;
        }
    }

    void ICommandContext.AutoLayout(ILaneLayout layout)
    {
        Editor.AutoLayout(layout);
    }

    bool ICommandContext.HasConnectors
    {
        get
        {
            return D.Edges.Count > 0;
        }
    }

    void ICommandContext.TidyConnectors()
    {
        Editor.TidyConnectors();
    }

    bool ICommandContext.HasOutsideText
    {
        get
        {
            return Editor.HasOutsideText;
        }
    }

    void ICommandContext.PlaceTextClear()
    {
        Editor.PlaceTextClear();
    }

    bool ICommandContext.HasLanes
    {
        get
        {
            return Editor.HasLanes;
        }
    }

    void ICommandContext.ArrangeLanes(ILaneLayout layout)
    {
        Editor.ArrangeLanes(layout);
    }

    void ICommandContext.Duplicate()
    {
        Editor.Duplicate();
    }

    void ICommandContext.DeleteSelection()
    {
        Editor.DeleteSelection();
    }

    void ICommandContext.GroupSelection()
    {
        Editor.GroupSelection();
    }

    void ICommandContext.UngroupSelected()
    {
        Group? g = Editor.SelectedGroup;
        if (g is not null)
        {
            Editor.Ungroup(g);
        }
    }

    void ICommandContext.JoinSelected()
    {
        Editor.JoinSelected();
    }

    bool ICommandContext.CanBranch
    {
        get
        {
            return Editor.BranchSource is not null;
        }
    }

    void ICommandContext.QuickBranch()
    {
        Editor.QuickBranch();
    }

    void ICommandContext.Nudge(double dx, double dy)
    {
        Editor.Nudge(dx, dy);
    }

    void ICommandContext.ToggleAngleLock()
    {
        Editor.ToggleAngleLock();
    }

    void ICommandContext.ToggleSelectedConnectorStyle()
    {
        Edge? e = Editor.SelectedEdge;
        if (e is not null)
        {
            if (e.Style == EdgeStyle.Straight)
            {
                Editor.SetEdgeStyle(e, EdgeStyle.Elbow);
            }
            else
            {
                Editor.SetEdgeStyle(e, EdgeStyle.Straight);
            }
        }
    }

    // Adding items

    void ICommandContext.AddShape(NodeType type)
    {
        Editor.QuickAdd(type);
    }

    void ICommandContext.AddText()
    {
        Editor.AddTextQuick();
    }

    void ICommandContext.AddComment()
    {
        Editor.AddCommentQuick();
    }

    void ICommandContext.AddGroup()
    {
        Editor.AddGroupQuick();
    }

    // View

    void ICommandContext.Fit()
    {
        Fit();
    }

    void ICommandContext.FitSelection()
    {
        FitSelection();
    }

    void ICommandContext.ZoomBy(double factor)
    {
        ZoomBy(factor);
    }

    void ICommandContext.ResetZoom()
    {
        Zoom = 1;
    }
}
