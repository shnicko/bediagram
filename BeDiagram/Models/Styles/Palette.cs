namespace BeDiagram.Models.Styles;

/// <summary>
/// The one colour set shared by shapes, groups, connectors and text boxes: 24 colours shown as a 3 × 8 grid.
/// Row 1 is the original eight, row 2 eight more hues, row 3 the soft sticky-note colours and the neutrals.
/// Diagrams save the key ("blue", "mint"), so a key's colours must not change once released.
/// </summary>
public static class Palette
{
    /// <summary>Label and icon colour on a solid fill that is too pale for white (white would be under 3:1 contrast).</summary>
    public const string DarkText = "#1f2933";
    const string White = "#ffffff";

    /// <summary>The colour used for an unknown or missing key.</summary>
    public const string DefaultColour = "blue";

    // Key, then Light, Stroke, Solid, OnSolid, Text — in picker order.
    static readonly (string Key, Swatch Swatch)[] Entries =
    {
        // Row 1: the original palette. Same colours as before; only amber's solid look now has a dark label.
        ("blue",     new Swatch("#eef3fb", "#3b6fb6", "#3b6fb6", White,    "#3b6fb6")),
        ("teal",     new Swatch("#e6f5f5", "#1f8796", "#1f8796", White,    "#1f8796")),
        ("green",    new Swatch("#e7f5ec", "#2f8f5b", "#2f8f5b", White,    "#2f8f5b")),
        ("amber",    new Swatch("#fff6e5", "#c58a12", "#d9930d", DarkText, "#c58a12")),
        ("red",      new Swatch("#fbe9e7", "#b4472f", "#c4513a", White,    "#b4472f")),
        ("pink",     new Swatch("#fdeef4", "#b8477a", "#c45086", White,    "#b8477a")),
        ("purple",   new Swatch("#f1ecfb", "#6a4bb5", "#6a4bb5", White,    "#6a4bb5")),
        ("slate",    new Swatch("#f1f3f5", "#55606e", "#4a5563", White,    "#55606e")),
        // Row 2: more hues.
        ("orange",   new Swatch("#fff0e6", "#c25a17", "#d0661f", White,    "#c25a17")),
        ("lime",     new Swatch("#f1f8e3", "#5f8a1a", "#6b9a1f", White,    "#5f8a1a")),
        ("cyan",     new Swatch("#e5f6fb", "#1a7fa1", "#1c8db3", White,    "#1a7fa1")),
        ("indigo",   new Swatch("#ecedfb", "#4549a8", "#4549a8", White,    "#4549a8")),
        ("magenta",  new Swatch("#fbebfa", "#a23aa0", "#a23aa0", White,    "#a23aa0")),
        ("brown",    new Swatch("#f5efe9", "#8a5a3b", "#8a5a3b", White,    "#8a5a3b")),
        ("olive",    new Swatch("#f4f4e4", "#6f6f24", "#7a7a2a", White,    "#6f6f24")),
        ("navy",     new Swatch("#e9edf5", "#26406e", "#26406e", White,    "#26406e")),
        // Row 3: sticky-note colours and neutrals. Light / Stroke of yellow, sky, mint, rose, white, grey and ink are
        // the text-box colours they always were; their Text is darker so a group title stays readable.
        ("yellow",   new Swatch("#fff7c2", "#d9c255", "#f2d24b", DarkText, "#7a6510")),
        ("sky",      new Swatch("#dcecff", "#7ea6d8", "#7ea6d8", DarkText, "#2f5f99")),
        ("mint",     new Swatch("#dcf5df", "#7fbf88", "#7fbf88", DarkText, "#2f7a3a")),
        ("rose",     new Swatch("#fde0ea", "#d88aa6", "#d88aa6", DarkText, "#9c3d61")),
        ("lavender", new Swatch("#ece6fb", "#a593d8", "#a593d8", DarkText, "#5b479c")),
        ("white",    new Swatch("#ffffff", "#cfd4da", "#ffffff", DarkText, "#55606e")),
        ("grey",     new Swatch("#f1f3f5", "#9aa3ad", "#9aa3ad", DarkText, "#55606e")),
        ("ink",      new Swatch("#efeee9", "#3d3a2a", "#3d3a2a", White,    "#3d3a2a")),
    };

    /// <summary>Every colour by key.</summary>
    public static readonly IReadOnlyDictionary<string, Swatch> All = BuildAll();

    /// <summary>Every key in picker order (row by row).</summary>
    public static readonly IReadOnlyList<string> Keys = BuildKeys();

    /// <summary>Colours handed out to new groups and swim lanes, in turn: the strong hues only, so neighbouring lanes
    /// are easy to tell apart.</summary>
    public static readonly IReadOnlyList<string> GroupOrder = new string[]
    {
        "blue", "teal", "green", "amber", "red", "pink", "purple", "slate",
        "orange", "lime", "cyan", "indigo", "magenta", "brown", "olive", "navy",
    };

    static Dictionary<string, Swatch> BuildAll()
    {
        Dictionary<string, Swatch> result = new Dictionary<string, Swatch>();
        foreach ((string key, Swatch swatch) in Entries)
        {
            result[key] = swatch;
        }
        return result;
    }

    static List<string> BuildKeys()
    {
        List<string> result = new List<string>();
        foreach ((string key, Swatch swatch) in Entries)
        {
            result.Add(key);
        }
        return result;
    }

    /// <summary>The colour for <paramref name="key"/>, or the default colour when the key is missing or unknown.</summary>
    public static Swatch Get(string? key)
    {
        if (key is not null && All.TryGetValue(key, out Swatch? s))
        {
            return s;
        }
        return All[DefaultColour];
    }

    /// <summary>The pale fill for <paramref name="key"/>, or null (no fill) when the key is missing or unknown.</summary>
    public static string? FillOf(string? key)
    {
        if (key is not null && All.TryGetValue(key, out Swatch? s))
        {
            return s.Light;
        }
        return null;
    }

    /// <summary>The line / text colour for <paramref name="key"/>, or <paramref name="fallback"/> when the key is
    /// missing or unknown.</summary>
    public static string StrokeOf(string? key, string fallback)
    {
        if (key is not null && All.TryGetValue(key, out Swatch? s))
        {
            return s.Stroke;
        }
        return fallback;
    }
}
