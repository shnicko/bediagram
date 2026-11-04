namespace BeDiagram.Models.Styles;

/// <summary>Shared colour set for shapes and groups. Light = pale fill, Stroke = outline/icon, Solid = filled style.</summary>
public static class Palette
{
    public static readonly IReadOnlyDictionary<string, Swatch> All = new Dictionary<string, Swatch>
    {
        ["blue"] = new("#eef3fb", "#3b6fb6", "#3b6fb6"),
        ["teal"] = new("#e6f5f5", "#1f8796", "#1f8796"),
        ["green"] = new("#e7f5ec", "#2f8f5b", "#2f8f5b"),
        ["amber"] = new("#fff6e5", "#c58a12", "#d9930d"),
        ["red"] = new("#fbe9e7", "#b4472f", "#c4513a"),
        ["pink"] = new("#fdeef4", "#b8477a", "#c45086"),
        ["purple"] = new("#f1ecfb", "#6a4bb5", "#6a4bb5"),
        ["slate"] = new("#f1f3f5", "#55606e", "#4a5563"),
    };

    public static Swatch Get(string? key) => key is not null && All.TryGetValue(key, out Swatch? s) ? s : All["blue"];
}
