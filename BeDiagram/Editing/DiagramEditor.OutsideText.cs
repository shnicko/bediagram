using BeDiagram.Editing.Labels;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Text;

namespace BeDiagram.Editing;

/// <summary>Where the text outside a shape sits (label below a circle or outcome, and the description): on a side of
/// the shape, dragged free with a leader line, or placed clear of connectors.</summary>
public partial class DiagramEditor
{
    // ───────────── Text outside the shape ─────────────

    /// <summary>Puts the text outside the shape on one side of it (underneath is the default), attached again if it was
    /// free. One undo step.</summary>
    public void SetLabelSide(Node n, Side side)
    {
        Side? stored = side;
        if (side == Side.Bottom)
        {
            stored = null;
        }
        if (n.FreeLabel is null && n.LabelSide == stored)
        {
            return;
        }
        Change();
        n.FreeLabel = null;
        n.LabelSide = stored;
        GrowAround(n);
    }

    /// <summary>Frees the text outside the shape where it is now, so it can be dragged anywhere (a leader line joins it
    /// to the shape once it's away from it). One undo step; nothing when it is already free or there is no such text.</summary>
    public void FreeText(Node n)
    {
        if (n.FreeLabel is not null)
        {
            return;
        }
        Offset? at = TextLayout.CurrentOffset(n);
        if (at is null)
        {
            return;
        }
        Change();
        n.FreeLabel = at;
    }

    /// <summary>True when any shape has text outside it that <see cref="PlaceTextClear"/> could move.</summary>
    public bool HasOutsideText
    {
        get
        {
            foreach (Node n in Diagram.Nodes)
            {
                if (TextLayout.HasOutsideText(n))
                {
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Moves the text outside shapes to the side with the fewest connectors through it (see <see cref="TextPlacer"/>):
    /// the selected shapes (and those in selected groups), or with nothing selected, every shape. Free text is left
    /// alone. One undo step, and none when nothing moves.
    /// </summary>
    public void PlaceTextClear()
    {
        List<Node> nodes = new List<Node>();
        string scope;
        if (Selection.Count > 0)
        {
            foreach (IBox b in WithMembers(Selection))
            {
                if (b is Node n)
                {
                    nodes.Add(n);
                }
            }
            scope = "the selected shapes";
        }
        else
        {
            nodes.AddRange(Diagram.Nodes);
            scope = "the shapes";
        }

        // Snapshot first, but only record it when something actually moved.
        string before = Snapshot();
        int moved = new TextPlacer(Diagram, id => FindBox(id)).Apply(nodes);
        if (moved == 0)
        {
            view.ShowToast($"Nothing to move — the text of {scope} is already as clear of connectors as it can be");
            return;
        }
        PushUndo(before);
        MarkDirty();
        foreach (Node n in nodes)
        {
            GrowAround(n);
        }
        string plural = "s";
        if (moved == 1)
        {
            plural = "";
        }
        view.ShowToast($"Moved the text of {moved} shape{plural} clear of connectors");
    }
}
