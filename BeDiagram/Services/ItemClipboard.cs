using BeDiagram.Editing;

namespace BeDiagram.Services;

/// <summary>
/// The copied items (Ctrl+C / Ctrl+X): one set at a time, not saved anywhere. A singleton like
/// <see cref="StyleClipboard"/>, so items copied in one tab or window can be pasted into another. Separate from the
/// browser's clipboard: copying text in a field is unaffected.
/// </summary>
public class ItemClipboard
{
    readonly object gate = new object();
    CopiedItems? held;

    /// <summary>The copied items, or null when nothing has been copied.</summary>
    public CopiedItems? Held
    {
        get
        {
            lock (gate)
            {
                return held;
            }
        }
    }

    public void Hold(CopiedItems items)
    {
        lock (gate)
        {
            held = items;
        }
    }
}
