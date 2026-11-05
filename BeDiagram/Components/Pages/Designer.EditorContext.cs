using BeDiagram.Components.Pages.Gestures;
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
    void IEditorContext.SelectOnly(string id) => Editor.SelectOnly(id);
    void IEditorContext.UpdateMembership(IEnumerable<IGroupable> items) => Editor.UpdateMembership(items);
    void IEditorContext.GrowAround(IGroupable item) => Editor.GrowAround(item);
    bool IEditorContext.AddEdge(string from, string to) => Editor.AddEdge(from, to);
    void IEditorContext.AddNodeAt(string type, double cx, double cy, bool join) => Editor.AddNodeAt(type, cx, cy, join);
    void IEditorContext.PanBy(double dx, double dy) { PanX += dx; PanY += dy; }
    string IEditorContext.Snapshot() => Editor.Snapshot();
    void IEditorContext.PushUndo(string snapshot) => Editor.PushUndo(snapshot);
    void IEditorContext.MarkDirty() => Editor.MarkDirty();
}
