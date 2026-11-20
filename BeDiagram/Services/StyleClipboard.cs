using BeDiagram.Models.Presets;

namespace BeDiagram.Services;

/// <summary>
/// The copied style (Copy style / the format painter): one at a time, not saved anywhere, gone when the app stops.
/// A singleton, like <see cref="StylePresetStore"/>: BeDiagram is a one-person local app, so every tab and every
/// browser window shares it, and a look copied in one diagram can be pasted in another. Also remembers the last
/// Paste special choice.
/// </summary>
public class StyleClipboard
{
    readonly object gate = new object();
    CopiedStyle? held;
    PasteParts lastSpecial = new PasteParts();

    /// <summary>The copied style, or null when nothing has been copied.</summary>
    public CopiedStyle? Held
    {
        get
        {
            lock (gate)
            {
                return held;
            }
        }
    }

    public void Hold(CopiedStyle style)
    {
        lock (gate)
        {
            held = style;
        }
    }

    public void Clear()
    {
        lock (gate)
        {
            held = null;
        }
    }

    /// <summary>What Paste special last added (a copy: the dialog edits its own).</summary>
    public PasteParts LastSpecial
    {
        get
        {
            lock (gate)
            {
                return lastSpecial.Clone();
            }
        }
        set
        {
            lock (gate)
            {
                lastSpecial = value.Clone();
            }
        }
    }
}
