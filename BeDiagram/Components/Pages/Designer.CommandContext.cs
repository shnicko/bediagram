using BeDiagram.Components.Pages.Commands;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Pages;

/// <summary>
/// The Designer as the receiver of editor commands (<see cref="ICommandContext"/>). Explicit implementations:
/// most forward to the Designer's own methods; the few actions that were only ever reached from a shortcut live here.
/// </summary>
public partial class Designer : ICommandContext
{
    bool ICommandContext.CanUndo => undo.Count > 0;
    bool ICommandContext.CanRedo => redo.Count > 0;
    bool ICommandContext.HasSelectedConnector => SelectedEdgeObj is not null;

    Task ICommandContext.SaveAsync() => Save();
    void ICommandContext.ShowSaveAs() => ShowSaveAs();
    void ICommandContext.Undo() => Undo();
    void ICommandContext.Redo() => Redo();

    void ICommandContext.SelectAll()
    {
        Sel.Clear();
        Sel.AddRange(D.Nodes.Select(n => n.Id));
        Sel.AddRange(D.TextBoxes.Select(c => c.Id));
        Sel.AddRange(D.Groups.Select(g => g.Id));
        SelEdge = null;
    }

    void ICommandContext.CancelAndClearSelection()
    {
        EditId = null;
        CancelGesture();
        ClearSelection();
    }

    void ICommandContext.EditSelected()
    {
        if (Sel.Count == 1) BeginEdit(Sel[0]);
        else if (SelEdge is not null) BeginEdit(SelEdge);
    }

    void ICommandContext.Duplicate() => Duplicate();
    void ICommandContext.DeleteSelection() => DeleteSelection();
    void ICommandContext.GroupSelection() => GroupSelection();

    void ICommandContext.UngroupSelected()
    {
        if (SelectedGroup is { } g) Ungroup(g);
    }

    void ICommandContext.JoinSelected() => JoinSelected();
    void ICommandContext.Nudge(double dx, double dy) => Nudge(dx, dy);

    void ICommandContext.ToggleSelectedConnectorStyle()
    {
        if (SelectedEdgeObj is { } e) SetEdgeStyle(e, e.Style == EdgeStyles.Straight ? EdgeStyles.Elbow : EdgeStyles.Straight);
    }

    void ICommandContext.AddShape(string type) => QuickAdd(type);
    void ICommandContext.AddText() => AddTextQuick();
    void ICommandContext.AddComment() => AddCommentQuick();
    void ICommandContext.AddGroup() => AddGroupQuick();

    void ICommandContext.Fit() => Fit();
    void ICommandContext.ZoomBy(double factor) => ZoomBy(factor);
    void ICommandContext.ResetZoom() => Zoom = 1;
}
