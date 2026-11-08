using BeDiagram.Editing.Angles;
using BeDiagram.Models;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>
/// What a gesture may use from the editor. The Designer implements it (Designer.EditorContext.cs), so gestures
/// depend on this short list rather than on the whole Designer.
/// </summary>
public interface IEditorContext
{
    Diagram Diagram { get; }

    /// <summary>Selected ids, in the order they were picked.</summary>
    List<string> Selection { get; }

    IBox? FindBox(string? id);
    Group? FindGroup(string? id);

    /// <summary>The boxes a connector runs between (a row of a list / IPO node where it ends on one), or null when either is gone.</summary>
    (IBox A, IBox B)? EdgeEnds(Edge e);

    /// <summary>Top-most item under a world point, ignoring <paramref name="exclude"/>.</summary>
    IBox? HitTest(double x, double y, string? exclude);

    /// <summary>Snaps a coordinate to the 10 px grid when Snap is on.</summary>
    double SnapV(double v);

    /// <summary>A drag offset after the angle lock (free, 90°, 45° or 30°).</summary>
    LockedOffset LockOffset(double dx, double dy);

    /// <summary>A point dragged from a start point, on an allowed angle and snapped where the lock allows.</summary>
    (double X, double Y) LockPoint(double fromX, double fromY, double x, double y);

    void SelectOnly(string id);

    /// <summary>Puts loose items into the group under their centre (after a drag).</summary>
    void UpdateMembership(IEnumerable<IGroupable> items);

    /// <summary>Grows the groups around an item so it still fits inside them (after a resize).</summary>
    void GrowAround(IGroupable item);

    /// <summary>Adds a connector (false when it isn't allowed or already exists). Doesn't record undo.</summary>
    bool AddEdge(string from, string to, string? fromItem, string? toItem);

    /// <summary>
    /// Adds a shape centred on a world point (records undo), joined from the selection when asked.
    /// With <paramref name="lockAngle"/>, a joined shape is placed on an allowed angle from its source.
    /// </summary>
    void AddNodeAt(string type, double cx, double cy, bool join, bool lockAngle);

    /// <summary>Moves the view by screen px.</summary>
    void PanBy(double dx, double dy);

    // Undo (Memento): a gesture takes a snapshot when it starts and pushes it if it changed anything.
    string Snapshot();
    void PushUndo(string snapshot);
    void MarkDirty();
}
