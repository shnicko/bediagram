namespace BeDiagram.Models.Styles;

/// <summary>
/// Colours for text boxes. Fill = background, Stroke = border and text. Includes the sticky-note
/// colours, white, greys and the shared shape <see cref="Palette"/>.
/// </summary>
public static class TextPalette
{
    public static readonly IReadOnlyDictionary<string, (string Fill, string Stroke)> All = BuildAll();

    static Dictionary<string, (string, string)> BuildAll()
    {
        Dictionary<string, (string, string)> d = new Dictionary<string, (string, string)>
        {
            ["white"] = ("#ffffff", "#cfd4da"),
            ["grey"] = ("#f1f3f5", "#9aa3ad"),
            ["ink"] = ("#3d3a2a", "#3d3a2a"),
            ["yellow"] = ("#fff7c2", "#d9c255"),
            ["sky"] = ("#dcecff", "#7ea6d8"),
            ["mint"] = ("#dcf5df", "#7fbf88"),
            ["rose"] = ("#fde0ea", "#d88aa6"),
        };
        foreach ((string k, Swatch s) in Palette.All) d[k] = (s.Light, s.Stroke);
        return d;
    }

    /// <summary>Swatches offered for backgrounds (pale colours).</summary>
    public static readonly string[] Fills = { "white", "grey", "yellow", "sky", "mint", "rose", "blue", "teal", "green", "amber", "red", "pink", "purple", "slate" };
    /// <summary>Swatches offered for borders and text (strong colours).</summary>
    public static readonly string[] Strokes = { "grey", "yellow", "sky", "mint", "rose", "blue", "teal", "green", "amber", "red", "pink", "purple", "slate", "ink" };

    public static string? FillOf(string? key) => key is not null && All.TryGetValue(key, out (string Fill, string Stroke) c) ? c.Fill : null;
    public static string StrokeOf(string? key, string fallback) => key is not null && All.TryGetValue(key, out (string Fill, string Stroke) c) ? c.Stroke : fallback;
}
