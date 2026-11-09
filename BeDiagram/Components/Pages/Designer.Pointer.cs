using BeDiagram.Components.Pages.Gestures;
using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Pages;

/// <summary>Pointer down / move / up on the canvas: picks the gesture (State pattern, see Gestures/) and hands it the moves.</summary>
public partial class Designer
{
    // ───────────── Pointer handling ─────────────

    /// <summary>Where the pointer is, in screen px and in world coordinates.</summary>
    GesturePoint PointAt(PointerEventArgs e)
    {
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        return new GesturePoint(e.ClientX, e.ClientY, x, y);
    }

    void ItemDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0) { StartPan(e); return; }
        if (Editor.EditingId is not null && Editor.EditingId != id) Editor.EndEdit();
        Editor.SelectedEdgeId = null;
        if (e.ShiftKey || e.CtrlKey)
        {
            if (Editor.Selection.Contains(id)) { Editor.Selection.Remove(id); return; }
            Editor.Selection.Add(id);
        }
        else if (!Editor.Selection.Contains(id))
        {
            Editor.SelectOnly(id);
        }
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new MoveGesture(this, x, y, Editor.WithMembers(Editor.Selection));
    }

    /// <summary>Pressing on a group's body: click selects the group, drag draws a selection box inside it.</summary>
    void GroupBodyDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0) { StartPan(e); return; }
        if (e.ShiftKey || e.CtrlKey) { ItemDown(e, id); return; }
        Editor.EndEdit();
        Editor.ClearSelection();
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new MarqueeGesture(this, x, y, clickSelects: id);
    }

    void PortDown(PointerEventArgs e, string id) => PortDown(e, id, null);

    /// <summary>Pressing a ● port: a shape's side, or (<paramref name="itemId"/>) a row of a list / IPO node.</summary>
    void PortDown(PointerEventArgs e, string id, string? itemId)
    {
        if (e.Button != 0) { StartPan(e); return; }
        Editor.EndEdit();
        Editor.SelectOnly(id);
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new ConnectGesture(this, x, y, id, itemId);
    }

    void ResizeDown(PointerEventArgs e, string id)
    {
        if (Editor.FindBox(id) is not { } box) return;
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new ResizeGesture(this, x, y, box);
    }

    void EdgeDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0) { StartPan(e); return; }
        if (Editor.EditingId is not null && Editor.EditingId != id) Editor.EndEdit();
        Editor.Selection.Clear();
        Editor.SelectedEdgeId = id;
    }

    /// <summary>Pressing a ● end of the selected connector: drag it to another side of its shape.</summary>
    void EdgeEndDown(PointerEventArgs e, string id, bool fromEnd)
    {
        if (e.Button != 0) { StartPan(e); return; }
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new EdgeEndGesture(this, x, y, id, fromEnd);
    }

    /// <summary>Pressing the ■ on an elbow connector's middle segment: slide that segment sideways.</summary>
    void BendDown(PointerEventArgs e, string id)
    {
        if (e.Button != 0) { StartPan(e); return; }
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        if (BendGesture.TryStart(this, x, y, id) is { } bend) gesture = bend;
    }

    void StartPan(PointerEventArgs e) => gesture = new PanGesture(this, e.ClientX, e.ClientY);

    void BgDown(PointerEventArgs e)
    {
        Editor.EndEdit();
        if (e.Button != 0) { StartPan(e); return; }
        if (!e.ShiftKey) Editor.ClearSelection();
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new MarqueeGesture(this, x, y);
    }

    void BgDoubleClick(MouseEventArgs e)
    {
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        Editor.AddNodeAt(NodeType.Process, x, y, join: false);
    }

    /// <summary>Hands the move to the current gesture; skips the render when nothing changed.</summary>
    void PointerMove(PointerEventArgs e)
    {
        if (gesture is null || !gesture.Move(PointAt(e))) skipRender = true;
    }

    void PointerUp(PointerEventArgs e)
    {
        if (gesture is null) { skipRender = true; return; }
        IGesture finished = gesture;
        gesture = null;
        finished.Up();
    }

    /// <summary>Stops the current gesture without pointer-up (Escape, inline editing).</summary>
    void CancelGesture()
    {
        gesture?.Cancel();
        gesture = null;
    }

    // What the canvas reads about the gesture in progress.
    MarqueeGesture? Marquee => gesture as MarqueeGesture;
    string SurfaceClass => gesture switch
    {
        PanGesture => "surface panning",
        ConnectGesture => "surface connecting",
        _ => "surface",
    };
}
