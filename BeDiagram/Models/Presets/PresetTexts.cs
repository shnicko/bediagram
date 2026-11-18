namespace BeDiagram.Models.Presets;

/// <summary>Reading a <see cref="PresetText"/> choice, and the choices offered when saving.</summary>
public static class PresetTexts
{
    /// <summary>The choices for a shape (label and description), with their names in the panel.</summary>
    public static readonly (PresetText Key, string Name)[] ForShape =
    {
        (PresetText.None, "None"),
        (PresetText.Label, "Label"),
        (PresetText.Description, "Description"),
        (PresetText.Both, "Both"),
    };

    /// <summary>The choices for a group, text box or connector, which have one text each.</summary>
    public static readonly (PresetText Key, string Name)[] ForOneText =
    {
        (PresetText.None, "None"),
        (PresetText.Label, "Text"),
    };

    /// <summary>True when the choice keeps the label (or the one text of a group, text box or connector).</summary>
    public static bool KeepsLabel(PresetText text)
    {
        return text == PresetText.Label || text == PresetText.Both;
    }

    /// <summary>True when the choice keeps a shape's description.</summary>
    public static bool KeepsDescription(PresetText text)
    {
        return text == PresetText.Description || text == PresetText.Both;
    }
}
