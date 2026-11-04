using BeDiagram.Components.Pages.Gestures;
using BeDiagram.Models;

namespace BeDiagram.Components.Pages;

/// <summary>
/// The Designer as seen by gestures (<see cref="IEditorContext"/>). Explicit implementations that forward to the
/// Designer's own members, so the gesture contract stays short and the Designer's members stay private.
/// </summary>
public partial class Designer : IEditorContext
{
    Diagram IEditorContext.Diagram => D;
    List<string> IEditorContext.Selection => Sel;
    IBox? IEditorContext.FindBox(string? id) => FindBox(id);
    Group? IEditorContext.FindGroup(string? id) => FindGroup(id);
    IBox? IEditorContext.HitTest(double x, double y, string? exclude) => HitTest(x, y, exclude);
    double IEditorContext.SnapV(double v) => SnapV(v);
    void IEditorContext.SelectOnly(string id) => SelectOnly(id);
    void IEditorContext.UpdateMembership(IEnumerable<IGroupable> items) => UpdateMembership(items);
    bool IEditorContext.AddEdge(string from, string to) => AddEdge(from, to);
    void IEditorContext.AddNodeAt(string type, double cx, double cy, bool join) => AddNodeAt(type, cx, cy, join);
    void IEditorContext.PanBy(double dx, double dy) { PanX += dx; PanY += dy; }
    string IEditorContext.Snapshot() => Snapshot();
    void IEditorContext.PushUndo(string snapshot) => PushUndo(snapshot);
    void IEditorContext.MarkDirty() => MarkDirty();
}
