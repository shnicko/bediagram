namespace BeDiagram.Components.Pages.Commands;

/// <summary>
/// A keyboard shortcut. <see cref="Key"/> is the browser's <c>KeyboardEvent.key</c>, lower-cased for single
/// characters ("d", "1", "+") and as-is for named keys ("Delete", "ArrowUp", "Escape"). Alt is only used by the tab
/// shortcuts (Alt+1…9, Alt+PageUp / PageDown, Alt+W, Alt+N): the browser keeps Ctrl+Tab, Ctrl+W and Ctrl+T for itself.
/// </summary>
public record KeyGesture(string Key, bool Ctrl = false, bool Shift = false, bool Alt = false)
{
    /// <summary>The gesture for a key press reported by designer.js.</summary>
    public static KeyGesture From(string key, bool ctrl, bool shift, bool alt)
    {
        string name = key;
        if (key.Length == 1)
        {
            name = key.ToLowerInvariant();
        }
        return new KeyGesture(name, ctrl, shift, alt);
    }

    public KeyGesture WithoutShift
    {
        get
        {
            return this with { Shift = false };
        }
    }

    /// <summary>Display text for tooltips, e.g. "Ctrl+Shift+S", "Alt+W", "F", "Delete".</summary>
    public override string ToString()
    {
        string key = Key;
        if (Key.Length == 1)
        {
            key = Key.ToUpperInvariant();
        }
        string text = "";
        if (Ctrl)
        {
            text += "Ctrl+";
        }
        if (Alt)
        {
            text += "Alt+";
        }
        if (Shift)
        {
            text += "Shift+";
        }
        return text + key;
    }
}
