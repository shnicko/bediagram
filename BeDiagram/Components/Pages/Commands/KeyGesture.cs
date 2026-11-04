namespace BeDiagram.Components.Pages.Commands;

/// <summary>
/// A keyboard shortcut. <see cref="Key"/> is the browser's <c>KeyboardEvent.key</c>, lower-cased for single
/// characters ("d", "1", "+") and as-is for named keys ("Delete", "ArrowUp", "Escape").
/// </summary>
public record KeyGesture(string Key, bool Ctrl = false, bool Shift = false)
{
    /// <summary>The gesture for a key press reported by designer.js.</summary>
    public static KeyGesture From(string key, bool ctrl, bool shift) =>
        new KeyGesture(key.Length == 1 ? key.ToLowerInvariant() : key, ctrl, shift);

    public KeyGesture WithoutShift => this with { Shift = false };

    /// <summary>Display text for tooltips, e.g. "Ctrl+Shift+S", "F", "Delete".</summary>
    public override string ToString()
    {
        string key = Key.Length == 1 ? Key.ToUpperInvariant() : Key;
        return (Ctrl ? "Ctrl+" : "") + (Shift ? "Shift+" : "") + key;
    }
}
