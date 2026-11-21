using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Components.Pages.Gestures;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Editing;
using BeDiagram.Editing.Guides;
using BeDiagram.Editing.Lanes;
using BeDiagram.Models;
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

    /// <summary>The head picker open on a connector end, or null (<see cref="HeadSpotDown"/>).</summary>
    HeadPick? headPick;

    void ItemDown(PointerEventArgs e, string id)
    {
        headPick = null;
        if (PaintAt(e, id))
        {
            return;
        }
        if (e.Button != 0)
        {
            StartPan(e);
            return;
        }
        if (Editor.EditingId is not null && Editor.EditingId != id)
        {
            Editor.EndEdit();
        }
        Editor.SelectedEdgeId = null;
        if (e.ShiftKey || e.CtrlKey)
        {
            if (Editor.Selection.Contains(id))
            {
                Editor.Selection.Remove(id);
                return;
            }
            Editor.Selection.Add(id);
        }
        else if (!Editor.Selection.Contains(id))
        {
            Editor.SelectOnly(id);
        }
        // Alt-drag moves group boxes without their contents; with shapes locked only group boxes move at all.
        MoveSet moving = Editor.MoveSetFor(Editor.Selection, e.AltKey);
        if (moving.Items.Count == 0)
        {
            return;
        }
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new MoveGesture(this, x, y, moving, id);
    }

    /// <summary>Pressing on a group's body: click selects the group, drag draws a selection box inside it.</summary>
    void GroupBodyDown(PointerEventArgs e, string id)
    {
        if (PaintAt(e, id))
        {
            return;
        }
        if (e.Button != 0)
        {
            StartPan(e);
            return;
        }
        if (e.ShiftKey || e.CtrlKey)
        {
            ItemDown(e, id);
            return;
        }
        Editor.EndEdit();
        Editor.ClearSelection();
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new MarqueeGesture(this, x, y, clickSelects: id);
    }

    /// <summary>Pressing a ● port: a shape's side, or (<paramref name="itemId"/>) a row of a list / IPO node.</summary>
    void PortDown(PointerEventArgs e, string id, string? itemId)
    {
        if (PaintAt(e, id))
        {
            return;
        }
        if (e.Button != 0)
        {
            StartPan(e);
            return;
        }
        Editor.EndEdit();
        Editor.SelectOnly(id);
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new ConnectGesture(this, x, y, id, itemId);
    }

    /// <summary>Pressing a shape's free text: selects the shape and drags just the text. With Shift / Ctrl it's the
    /// same as pressing the shape (add to or take from the selection).</summary>
    void OutsideTextDown(PointerEventArgs e, string id)
    {
        if (PaintAt(e, id))
        {
            return;
        }
        if (e.Button != 0)
        {
            StartPan(e);
            return;
        }
        if (e.ShiftKey || e.CtrlKey)
        {
            ItemDown(e, id);
            return;
        }
        if (Editor.FindBox(id) is not Node n || n.FreeLabel is null || Editor.ShapesLocked)
        {
            ItemDown(e, id);
            return;
        }
        if (Editor.EditingId is not null && Editor.EditingId != id)
        {
            Editor.EndEdit();
        }
        Editor.SelectedEdgeId = null;
        Editor.SelectOnly(id);
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new FreeTextGesture(this, x, y, n);
    }

    void ResizeDown(PointerEventArgs e, string id, ResizeHandle handle)
    {
        IBox? box = Editor.FindBox(id);
        if (box is null)
        {
            return;
        }
        // Locked shapes and text boxes keep their size too; group boxes can still be resized.
        if (Editor.ShapesLocked && box is not Group)
        {
            return;
        }
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new ResizeGesture(this, x, y, box, handle, e.AltKey);
    }

    /// <summary>Pressing the edge two touching lanes share: drag it to resize both lanes.</summary>
    void DividerDown(PointerEventArgs e, LaneDivider d)
    {
        if (e.Button != 0)
        {
            StartPan(e);
            return;
        }
        Editor.EndEdit();
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        gesture = new LaneDividerGesture(this, x, y, d);
    }

    void EdgeDown(PointerEventArgs e, string id)
    {
        headPick = null;
        if (PaintAt(e, id))
        {
            return;
        }
        if (e.Button != 0)
        {
            StartPan(e);
            return;
        }
        if (Editor.EditingId is not null && Editor.EditingId != id)
        {
            Editor.EndEdit();
        }
        Editor.Selection.Clear();
        Editor.SelectedEdgeId = id;
    }

    /// <summary>Pressing a connector's quick-change spot: select the connector and open the head picker for that end
    /// where the spot is (a second press on the same spot closes it).</summary>
    void HeadSpotDown(PointerEventArgs e, string id, bool atStart)
    {
        if (e.Button != 0)
        {
            StartPan(e);
            return;
        }
        Editor.EndEdit();
        Editor.Selection.Clear();
        Editor.SelectedEdgeId = id;
        if (headPick is not null && headPick.EdgeId == id && headPick.AtStart == atStart)
        {
            headPick = null;
            return;
        }
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        headPick = new HeadPick(id, atStart, x, y);
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
        headPick = null;
        Editor.EndEdit();
        if (e.Button != 0)
        {
            StartPan(e);
            return;
        }
        // A click on empty canvas puts the single-shot format painter away (the sticky one waits for Esc).
        if (painting && !paintSticky)
        {
            StopPainter();
        }
        if (!e.ShiftKey)
        {
            Editor.ClearSelection();
        }
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

    /// <summary>The centre guides to draw while a drag lines the grabbed item up with other shapes.</summary>
    List<GuideLine> DragGuides
    {
        get
        {
            if (gesture is MoveGesture move)
            {
                return move.Guides;
            }
            return new List<GuideLine>();
        }
    }
    string SurfaceClass
    {
        get
        {
            if (gesture is PanGesture)
            {
                return "surface panning";
            }
            if (gesture is ConnectGesture)
            {
                return "surface connecting";
            }
            if (painting)
            {
                return "surface painting";
            }
            return "surface";
        }
    }
}
