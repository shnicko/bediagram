using BeDiagram.Models.Serialization;
using System.Globalization;
using BeDiagram.Models;

namespace BeDiagram.Editing;

/// <summary>Text boxes, node properties and descriptions, and inline label / text editing.</summary>
public partial class DiagramEditor
{
    // ───────────── Text boxes and properties ─────────────


    /// <summary>Turning the comment flag on applies the standard comment look; turning it off keeps the look.</summary>
    public void SetComment(TextBox t, bool on)
    {
        if (t.IsComment == on) return;
        Change();
        if (on) t.ApplyCommentStyle(); else t.IsComment = false;
        foreach (Edge e in Diagram.Edges.Where(e => e.From == t.Id || e.To == t.Id))
        {
            if (on) TextBox.ApplyPinLook(e);
            else { e.CopyLookFrom(Diagram.DefaultLook); e.Style = Diagram.DefaultConnector; }
        }
        // Comments can't be pinned to other comments.
        if (on) Diagram.Edges.RemoveAll(e => (e.From == t.Id && FindBox(e.To) is TextBox { IsComment: true }) || (e.To == t.Id && FindBox(e.From) is TextBox { IsComment: true }));
    }

    public void ResetCommentStyle(TextBox t) { Change(); t.ApplyCommentStyle(); }

    /// <summary>Sets the height so the wrapped text just fits.</summary>
    public void FitTextHeight(TextBox t)
    {
        Change();
        t.H = TextHeight(t);
        GrowAround(t);
    }

    /// <summary>
    /// Makes a text box taller if its text no longer fits; never shrinks it (a box made tall on purpose stays tall).
    /// No undo step of its own: it runs after typing, which already has one. The groups around it grow too.
    /// </summary>
    public void GrowToText(TextBox t)
    {
        double h = TextHeight(t);
        if (h <= t.H) return;
        t.H = h;
        Dirty = true;
        GrowAround(t);
    }

    /// <summary>The height at which the wrapped text just fits.</summary>
    static double TextHeight(TextBox t) => Math.Max(24, Math.Ceiling(Geometry.Layout(t).ContentHeight / 2) * 2);

    public void SetFontSize(IReadOnlyList<TextBox> boxes, object? value)
    {
        if (boxes.Count == 0 || !double.TryParse(value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) return;
        Change();
        foreach (TextBox t in boxes) t.FontSize = Math.Clamp(v, 6, 96);
    }

    public void AddProp(Node n)
    {
        Change();
        int i = 1;
        while (n.Properties.ContainsKey($"prop{i}")) i++;
        n.Properties[$"prop{i}"] = "";
    }

    public void RenameProp(Node n, string oldKey, string? newKey)
    {
        newKey = newKey?.Trim();
        if (string.IsNullOrEmpty(newKey) || newKey == oldKey || n.Properties.ContainsKey(newKey)) return;
        // Rebuild to keep ordering stable.
        Dictionary<string, string> rebuilt = n.Properties.ToDictionary(kv => kv.Key == oldKey ? newKey : kv.Key, kv => kv.Value);
        n.Properties = rebuilt;
        Dirty = true;
    }

    public void RemoveProp(Node n, string key) { Checkpoint(); n.Properties.Remove(key); }

    // ───────────── Inline text editing ─────────────

    public void BeginEdit(string id) => BeginEdit(id, checkpoint: true);

    public void BeginEdit(string id, bool checkpoint)
    {
        if (checkpoint) Checkpoint();
        EditingId = id;
        if (Diagram.Edges.Any(e => e.Id == id)) { Selection.Clear(); SelectedEdgeId = id; }
        else if (!Selection.Contains(id)) SelectOnly(id);
        view.EditStarted();
    }

    public void EndEdit()
    {
        if (EditingId is null) return;
        if (Diagram.Nodes.FirstOrDefault(n => n.Id == EditingId) is { } n) n.Label = n.Label.TrimEnd('\n', '\r');
        if (Diagram.TextBoxes.FirstOrDefault(t => t.Id == EditingId) is { } tb) GrowToText(tb);
        if (Diagram.Edges.FirstOrDefault(x => x.Id == EditingId) is { } edge) edge.Label = Geometry.ClampEdgeLabel(edge.Label).Trim();
        EditingId = null;
    }
}
