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
    Diagram IEditorContext.Diagram => D;
    List<string> IEditorContext.Selection => Editor.Selection;
    IBox? IEditorContext.FindBox(string? id) => Editor.FindBox(id);
    Group? IEditorContext.FindGroup(string? id) => Editor.FindGroup(id);
    IBox? IEditorContext.HitTest(double x, double y, string? exclude) => Editor.HitTest(x, y, exclude);
    double IEditorContext.SnapV(double v) => Editor.SnapV(v);
    LockedOffset IEditorContext.LockOffset(double dx, double dy) => Editor.LockOffset(dx, dy);
    (double X, double Y) IEditorContext.LockPoint(double fromX, double fromY, double x, double y) => Editor.LockPoint(fromX, fromY, x, y);
    void IEditorContext.SelectOnly(string id) => Editor.SelectOnly(id);
    void IEditorContext.UpdateMembership(IEnumerable<IGroupable> items) => Editor.UpdateMembership(items);
    void IEditorContext.GrowAround(IGroupable item) => Editor.GrowAround(item);
    bool IEditorContext.AddEdge(string from, string to, string? fromItem, string? toItem) => Editor.AddEdge(from, to, fromItem: fromItem, toItem: toItem);
    (IBox A, IBox B)? IEditorContext.EdgeEnds(Edge e) => Editor.Ends(e);
    void IEditorContext.AddNodeAt(string type, double cx, double cy, bool join, bool lockAngle) =>
        Editor.AddNodeAt(type, cx, cy, join, lockAngle: lockAngle);
    void IEditorContext.PanBy(double dx, double dy) { PanX += dx; PanY += dy; }
    string IEditorContext.Snapshot() => Editor.Snapshot();
    void IEditorContext.PushUndo(string snapshot) => Editor.PushUndo(snapshot);
    void IEditorContext.MarkDirty() => Editor.MarkDirty();
}
