using BeDiagram.Components.Pages.Commands;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Pages;

/// <summary>
/// The Designer as the receiver of editor commands (<see cref="ICommandContext"/>). Explicit implementations:
/// diagram actions forward to the <see cref="Editing.DiagramEditor"/>; saving, dialogs, zoom and cancelling a drag are
/// the page's own.
/// </summary>
public partial class Designer : ICommandContext
{
    bool ICommandContext.CanUndo => Editor.CanUndo;
    bool ICommandContext.CanRedo => Editor.CanRedo;
    bool ICommandContext.HasSelectedConnector => Editor.SelectedEdge is not null;
    bool ICommandContext.HasSelection => Editor.Selection.Count > 0 || Editor.SelectedEdge is not null;

    Task ICommandContext.SaveAsync() => Save();
    void ICommandContext.ShowSaveAs() => ShowSaveAs();
    void ICommandContext.Undo() => Editor.Undo();
    void ICommandContext.Redo() => Editor.Redo();

    void ICommandContext.SelectAll() => Editor.SelectAll();

    void ICommandContext.CancelAndClearSelection()
    {
        Editor.EditingId = null;
        CancelGesture();
        Editor.ClearSelection();
    }

    void ICommandContext.EditSelected()
    {
        if (Editor.Selection.Count == 1) Editor.BeginEdit(Editor.Selection[0]);
        else if (Editor.SelectedEdgeId is not null) Editor.BeginEdit(Editor.SelectedEdgeId);
    }

    void ICommandContext.Duplicate() => Editor.Duplicate();
    void ICommandContext.DeleteSelection() => Editor.DeleteSelection();
    void ICommandContext.GroupSelection() => Editor.GroupSelection();

    void ICommandContext.UngroupSelected()
    {
        if (Editor.SelectedGroup is { } g) Editor.Ungroup(g);
    }

    void ICommandContext.JoinSelected() => Editor.JoinSelected();
    void ICommandContext.Nudge(double dx, double dy) => Editor.Nudge(dx, dy);

    void ICommandContext.ToggleSelectedConnectorStyle()
    {
        if (Editor.SelectedEdge is { } e) Editor.SetEdgeStyle(e, e.Style == EdgeStyles.Straight ? EdgeStyles.Elbow : EdgeStyles.Straight);
    }

    void ICommandContext.AddShape(string type) => Editor.QuickAdd(type);
    void ICommandContext.AddText() => Editor.AddTextQuick();
    void ICommandContext.AddComment() => Editor.AddCommentQuick();
    void ICommandContext.AddGroup() => Editor.AddGroupQuick();

    void ICommandContext.Fit() => Fit();
    void ICommandContext.FitSelection() => FitSelection();
    void ICommandContext.ZoomBy(double factor) => ZoomBy(factor);
    void ICommandContext.ResetZoom() => Zoom = 1;
}
