namespace BeDiagram.Models.Presets;

/// <summary>
/// What Copy style (and the format painter) holds: one item's look, like a saved style with no name that lasts until
/// something else is copied. <see cref="Look"/> is what an ordinary paste copies, the same as applying a saved style.
/// A copy of the item itself (<see cref="Shape"/>, <see cref="Group"/>, <see cref="TextBox"/> or
/// <see cref="Connector"/>, exactly one set) keeps the extra parts Paste special can add: size, type, text,
/// properties and link. Kept by the <c>StyleClipboard</c> service, outside any diagram, so it can be pasted into
/// another tab or window.
/// </summary>
public class CopiedStyle
{
    CopiedStyle(StylePreset look, string kind, string name)
    {
        Look = look;
        Kind = kind;
        Name = name;
    }

    /// <summary>The look only (no text), as a saved style would hold it.</summary>
    public StylePreset Look { get; }

    /// <summary>What kind of item it came from, for messages: "shape", "group", "text box" or "connector".</summary>
    public string Kind { get; }

    /// <summary>The item's text when it was copied, for the status bar (may be empty).</summary>
    public string Name { get; }

    public Node? Shape { get; private set; }
    public Group? Group { get; private set; }
    public TextBox? TextBox { get; private set; }
    public Edge? Connector { get; private set; }

    public static CopiedStyle From(Node n)
    {
        StylePreset look = new StylePreset();
        look.Shape = ShapePreset.From(n, PresetText.None);
        CopiedStyle c = new CopiedStyle(look, "shape", n.Label);
        c.Shape = n.Copy();
        return c;
    }

    public static CopiedStyle From(Group g)
    {
        StylePreset look = new StylePreset();
        look.Group = GroupPreset.From(g, PresetText.None);
        CopiedStyle c = new CopiedStyle(look, "group", g.Title);
        c.Group = g.Copy();
        return c;
    }

    public static CopiedStyle From(TextBox t)
    {
        StylePreset look = new StylePreset();
        look.TextBox = TextBoxPreset.From(t, PresetText.None);
        CopiedStyle c = new CopiedStyle(look, "text box", t.Text);
        c.TextBox = t.Copy();
        return c;
    }

    public static CopiedStyle From(Edge e)
    {
        StylePreset look = new StylePreset();
        look.Connector = ConnectorPreset.From(e, PresetText.None);
        CopiedStyle c = new CopiedStyle(look, "connector", e.Label);
        // Only the label is read back from the copy (Paste special → Text).
        Edge copy = new Edge();
        copy.Label = e.Label;
        copy.CopyLookFrom(e);
        c.Connector = copy;
        return c;
    }

    /// <summary>Plural of <see cref="Kind"/>, for "applied to 3 shapes".</summary>
    public string KindPlural
    {
        get
        {
            if (Kind == "text box")
            {
                return "text boxes";
            }
            return Kind + "s";
        }
    }

    /// <summary>A short description for the status bar: the kind and the start of its text.</summary>
    public string Summary
    {
        get
        {
            string text = Name.Replace('\n', ' ').Trim();
            if (text.Length > 24)
            {
                text = text.Substring(0, 23) + "…";
            }
            if (text.Length == 0)
            {
                return Kind;
            }
            return $"{Kind} “{text}”";
        }
    }
}
