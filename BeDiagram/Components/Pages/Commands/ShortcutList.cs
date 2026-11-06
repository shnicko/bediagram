namespace BeDiagram.Components.Pages.Commands;

/// <summary>
/// The keyboard shortcuts list (Help → Keyboard shortcuts), built from <see cref="CommandRegistry"/> so it can't
/// drift from what the keys actually do, plus the mouse gestures, which aren't commands.
/// </summary>
public static class ShortcutList
{
    /// <summary>Mouse gestures, listed after the keys.</summary>
    public static readonly IReadOnlyList<ShortcutRow> Mouse = new List<ShortcutRow>
    {
        new("Drag a ● port", "Connect · drop on empty space for a new step"),
        new("Shift+click / drag a box", "Select several (drag inside a group selects its items)"),
        new("Double-click", "Edit text (on a connector: its label)"),
        new("Wheel · right-drag", "Pan"),
        new("Ctrl+wheel", "Zoom"),
    };

    /// <summary>
    /// One row per command with a shortcut, in the registry's order. The nine add-shape keys and the arrow-key nudges
    /// are each folded into one row.
    /// </summary>
    public static List<ShortcutRow> Keys(CommandRegistry registry)
    {
        List<ShortcutRow> rows = new List<ShortcutRow>();
        bool shapes = false, nudge = false;
        foreach (IEditorCommand c in registry.All)
        {
            if (c.Keys.Count == 0) continue;
            if (c is AddShapeCommand)
            {
                if (!shapes) rows.Add(new ShortcutRow("1–9", "Add a shape (joined to the selection when Auto-join is on)"));
                shapes = true;
            }
            else if (c is NudgeCommand)
            {
                if (!nudge) rows.Add(new ShortcutRow("Arrow keys", "Nudge the selection (Shift = 1 px)"));
                nudge = true;
            }
            else
            {
                rows.Add(new ShortcutRow(string.Join(" / ", c.Keys.Select(k => k.ToString())), c.Name));
            }
        }
        return rows;
    }
}
