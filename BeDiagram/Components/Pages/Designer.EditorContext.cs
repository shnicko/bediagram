using BeDiagram.Components.Pages.Gestures;
using BeDiagram.Editing.Angles;
using BeDiagram.Models;

namespace BeDiagram.Components.Pages;

/// <summary>
/// The Designer as seen by gestures (<see cref="IEditorContext"/>). Explicit implementations that forward to the
/// <see cref="Editing.DiagramEditor"/>; panning is the page's own. Keeps the gesture contract short.
/// </summary>
public partial class Designer : IEditorContext
{
    Diagram IEditorContext.Diagram
    {
        get
        {
            return D;
        }
    }

    List<string> IEditorContext.Selection
    {
        get
        {
            return Editor.Selection;
        }
    }

    IBox? IEditorContext.FindBox(string? id)
    {
        return Editor.FindBox(id);
    }

    Group? IEditorContext.FindGroup(string? id)
    {
        return Editor.FindGroup(id);
    }

    IBox? IEditorContext.HitTest(double x, double y, string? exclude)
    {
        return Editor.HitTest(x, y, exclude);
    }

    double IEditorContext.SnapV(double v)
    {
        return Editor.SnapV(v);
    }

    LockedOffset IEditorContext.LockOffset(double dx, double dy)
    {
        return Editor.LockOffset(dx, dy);
    }

    (double X, double Y) IEditorContext.LockPoint(double fromX, double fromY, double x, double y)
    {
        return Editor.LockPoint(fromX, fromY, x, y);
    }

    void IEditorContext.SelectOnly(string id)
    {
        Editor.SelectOnly(id);
    }

    void IEditorContext.UpdateMembership(IEnumerable<IGroupable> items)
    {
        Editor.UpdateMembership(items);
    }

    void IEditorContext.GrowAround(IGroupable item)
    {
        Editor.GrowAround(item);
    }

    bool IEditorContext.AddEdge(string from, string to, string? fromItem, string? toItem)
    {
        return Editor.AddEdge(from, to, fromItem: fromItem, toItem: toItem);
    }

    (IBox A, IBox B)? IEditorContext.EdgeEnds(Edge e)
    {
        return Editor.Ends(e);
    }

    void IEditorContext.AddNodeAt(PaletteItem item, double cx, double cy, bool join, bool lockAngle)
    {
        Editor.AddNodeAt(item, cx, cy, join, lockAngle: lockAngle);
    }

    void IEditorContext.PanBy(double dx, double dy)
    {
        PanX += dx;
        PanY += dy;
    }

    string IEditorContext.Snapshot()
    {
        return Editor.Snapshot();
    }

    void IEditorContext.PushUndo(string snapshot)
    {
        Editor.PushUndo(snapshot);
    }

    void IEditorContext.MarkDirty()
    {
        Editor.MarkDirty();
    }
}
