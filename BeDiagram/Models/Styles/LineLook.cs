namespace BeDiagram.Models.Styles;

/// <summary>Line weight, dash pattern, colour and arrowheads for connectors.</summary>
public static class LineLook
{
    public const string Light = "light", Regular = "regular", Heavy = "heavy";
    public const string Solid = "solid", Dashed = "dashed", Dotted = "dotted", DashDot = "dashdot", LongDash = "longdash";
    public const string ArrowEnd = "end", ArrowBoth = "both", ArrowNone = "none";
    public const string DefaultColor = "#55606e";

    public static readonly (string Key, string Name)[] Weights = { (Light, "Light"), (Regular, "Regular"), (Heavy, "Heavy") };
    public static readonly (string Key, string Name)[] Patterns =
        { (Solid, "Solid"), (Dashed, "Dashed"), (Dotted, "Dotted"), (DashDot, "Dash-dot"), (LongDash, "Long dash") };
    public static readonly (string Key, string Name)[] ArrowModes = { (ArrowEnd, "End"), (ArrowBoth, "Both"), (ArrowNone, "None") };

    public static double Width(string weight) => weight switch { Light => 1, Heavy => 3, _ => 1.6 };

    public static string? Dash(string pattern, double w) => pattern switch
    {
        Dashed => $"{N(5 + 2 * w)} {N(3 + 1.5 * w)}",
        Dotted => $"0 {N(2 + 2 * w)}",
        DashDot => $"{N(7 + 2 * w)} {N(3 + 1.5 * w)} 0 {N(3 + 1.5 * w)}",
        LongDash => $"{N(12 + 3 * w)} {N(4 + 2 * w)}",
        _ => null,
    };

    /// <summary>Round caps turn zero-length dashes into dots.</summary>
    public static string Cap(string pattern) => pattern is Dotted or DashDot ? "round" : "butt";

    public static string Colour(string? key) =>
        key is not null && Palette.All.TryGetValue(key, out Swatch? sw) ? sw.Stroke : TextPalette.StrokeOf(key, DefaultColor);

    /// <summary>Marker id for an arrowhead in a given colour and weight (one marker per combination).</summary>
    public static string MarkerId(string colour, string weight) => $"arr-{colour.TrimStart('#')}-{weight}";

    /// <summary>Arrowhead marker sized in user units so heavy lines don't get giant arrows.</summary>
    public static string MarkerDef(string colour, string weight)
    {
        int size = weight switch { Light => 8, Heavy => 13, _ => 10 };
        return $"<marker id=\"{MarkerId(colour, weight)}\" viewBox=\"0 0 10 10\" refX=\"9\" refY=\"5\" markerUnits=\"userSpaceOnUse\" markerWidth=\"{size}\" markerHeight=\"{size}\" orient=\"auto-start-reverse\"><path d=\"M0,0 L10,5 L0,10 z\" fill=\"{colour}\"/></marker>";
    }

    static string N(double v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
}
