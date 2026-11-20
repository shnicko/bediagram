using BeDiagram.Models;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Presets;

namespace BeDiagram.Editing;

/// <summary>
/// Copy style / paste style and the format painter: a temporary, unnamed saved style (<see cref="CopiedStyle"/>).
/// An ordinary paste copies the look, the same as applying a saved style; Paste special can add size, type, text,
/// properties and link (<see cref="PasteParts"/>). Pasting goes onto the selected items of the same kind as the
/// copied one (others are skipped) and is one undo step.
/// </summary>
public partial class DiagramEditor
{
    // ───────────── Copy ─────────────

    /// <summary>The style of the one selected item or connector, or null when there isn't exactly one.</summary>
    public CopiedStyle? CopyStyleOfSelection()
    {
        Edge? e = SelectedEdge;
        if (e is not null)
        {
            return CopiedStyle.From(e);
        }
        if (Selection.Count != 1)
        {
            return null;
        }
        return CopyStyleOf(Selection[0]);
    }

    /// <summary>The style of the shape, group, text box or connector <paramref name="id"/>, or null.</summary>
    public CopiedStyle? CopyStyleOf(string id)
    {
        IBox? box = FindBox(id);
        if (box is Node n)
        {
            return CopiedStyle.From(n);
        }
        if (box is Group g)
        {
            return CopiedStyle.From(g);
        }
        if (box is TextBox t)
        {
            return CopiedStyle.From(t);
        }
        Edge? e = FindEdge(id);
        if (e is not null)
        {
            return CopiedStyle.From(e);
        }
        return null;
    }

    // ───────────── Paste ─────────────

    /// <summary>
    /// Pastes <paramref name="style"/> onto the selection: every selected item of the same kind (or the selected
    /// connector, for a connector style). Returns how many items it went onto and how many were skipped because they
    /// are another kind. Nothing changes (and no undo step) when it goes onto none.
    /// </summary>
    public (int Applied, int Skipped) PasteStyle(CopiedStyle style, PasteParts parts)
    {
        List<string> targets = new List<string>();
        int skipped = 0;
        if (style.Connector is not null)
        {
            if (SelectedEdgeId is not null)
            {
                targets.Add(SelectedEdgeId);
            }
            skipped = Selection.Count;
        }
        else
        {
            foreach (string id in Selection)
            {
                if (SameKind(style, id))
                {
                    targets.Add(id);
                }
                else
                {
                    skipped++;
                }
            }
        }
        if (targets.Count == 0)
        {
            return (0, skipped);
        }
        AsOneStep(() =>
        {
            foreach (string id in targets)
            {
                PasteOnto(style, parts, id);
            }
        });
        return (targets.Count, skipped);
    }

    /// <summary>The format painter's click: the look of <paramref name="style"/> onto item or connector
    /// <paramref name="id"/>. False (nothing changed) when it is another kind.</summary>
    public bool PaintStyle(CopiedStyle style, string id)
    {
        if (!SameKind(style, id))
        {
            return false;
        }
        AsOneStep(() =>
        {
            PasteOnto(style, PasteParts.LookOnly(), id);
        });
        return true;
    }

    /// <summary>True when <paramref name="id"/> is the same kind of item the style was copied from.</summary>
    public bool SameKind(CopiedStyle style, string id)
    {
        if (style.Connector is not null)
        {
            return FindEdge(id) is not null;
        }
        IBox? box = FindBox(id);
        if (style.Shape is not null)
        {
            return box is Node;
        }
        if (style.Group is not null)
        {
            return box is Group;
        }
        return box is TextBox;
    }

    Edge? FindEdge(string id)
    {
        foreach (Edge e in Diagram.Edges)
        {
            if (e.Id == id)
            {
                return e;
            }
        }
        return null;
    }

    void PasteOnto(CopiedStyle style, PasteParts parts, string id)
    {
        IBox? box = FindBox(id);
        if (style.Shape is not null && box is Node n)
        {
            PasteOntoShape(style.Shape, style.Look.Shape!, parts, n);
        }
        else if (style.Group is not null && box is Group g)
        {
            PasteOntoGroup(style.Group, style.Look.Group!, parts, g);
        }
        else if (style.TextBox is not null && box is TextBox t)
        {
            PasteOntoTextBox(style.TextBox, style.Look.TextBox!, parts, t);
        }
        else if (style.Connector is not null)
        {
            Edge? e = FindEdge(id);
            if (e is not null)
            {
                style.Look.Connector!.CopyTo(e);
                if (parts.Text)
                {
                    e.Label = style.Connector.Label;
                }
            }
        }
    }

    void PasteOntoShape(Node from, ShapePreset look, PasteParts parts, Node n)
    {
        // Type first: changing type also sets the size and may rename the label, which the other parts then override.
        if (parts.Type)
        {
            ChangeType(n, from.Type);
            if (n.Kind.HasOutcome && from.Outcome is not null)
            {
                SetOutcome(n, from.Outcome.Value);
            }
            if (n.Kind.CanBeRound && n.Round != from.Round)
            {
                SetRound(n, from.Round);
            }
        }
        look.CopyTo(n);
        if (parts.Text)
        {
            n.Label = from.Label;
            n.Description = from.Description;
        }
        if (parts.Properties)
        {
            n.Properties = new Dictionary<string, string>(from.Properties);
        }
        if (parts.Link)
        {
            n.Link = from.Link;
        }
        if (parts.Size)
        {
            SetSize(n, from.W, from.H);
        }
        GrowAround(n);
    }

    void PasteOntoGroup(Group from, GroupPreset look, PasteParts parts, Group g)
    {
        look.CopyTo(g);
        if (parts.Text)
        {
            g.Title = from.Title;
        }
        if (parts.Link)
        {
            g.Link = from.Link;
        }
        if (parts.Size)
        {
            SetSize(g, from.W, from.H);
        }
    }

    void PasteOntoTextBox(TextBox from, TextBoxPreset look, PasteParts parts, TextBox t)
    {
        look.CopyTo(t);
        if (parts.Text)
        {
            t.Text = from.Text;
        }
        if (parts.Size)
        {
            SetSize(t, from.W, from.H);
        }
        GrowToText(t);
    }

    /// <summary>A new width and height through the item's placement (keeps its set-out point, as typing a size in the
    /// panel does).</summary>
    void SetSize(IGroupable item, double w, double h)
    {
        Rect fields = item.Placement.Fields(item);
        SetBounds(item, new Rect(fields.X, fields.Y, w, h));
    }

    /// <summary>
    /// Runs <paramref name="actions"/> as one undo step. The editing methods it calls (ChangeType, SetBounds, …) each
    /// take their own undo snapshot; those are dropped again, keeping only the one taken before the first change.
    /// </summary>
    void AsOneStep(Action actions)
    {
        Change();
        string first = undo[undo.Count - 1];
        actions();
        // Find the snapshot taken above (by reference: a later snapshot of an unchanged diagram has the same text).
        int at = -1;
        for (int i = undo.Count - 1; i >= 0; i--)
        {
            if (ReferenceEquals(undo[i], first))
            {
                at = i;
                break;
            }
        }
        if (at >= 0)
        {
            undo.RemoveRange(at + 1, undo.Count - at - 1);
        }
        Dirty = true;
    }
}
