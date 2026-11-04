namespace BeDiagram.Models.Styles;

public static class TextLook
{
    public const string NoBorder = "none";
    public const string Sans = "sans", Serif = "serif", Mono = "mono";
    public const string Left = "left", Center = "center", Right = "right";
    public const string Top = "top", Middle = "middle", Bottom = "bottom";

    public static readonly (string Key, string Name)[] Fonts = { (Sans, "Sans"), (Serif, "Serif"), (Mono, "Mono") };
    public static readonly double[] Sizes = { 10, 11, 12, 13, 14, 16, 18, 20, 24, 28, 32, 40 };

    public static string Family(string font) => font switch
    {
        Mono => "Consolas, 'Cascadia Mono', 'Courier New', monospace",
        Serif => "Georgia, 'Times New Roman', serif",
        _ => "'Segoe UI', Arial, sans-serif",
    };

    /// <summary>Average character width as a fraction of the font size (used for wrapping).</summary>
    public static double CharWidth(string font) => font switch { Mono => 0.56, Serif => 0.52, _ => 0.54 };

    public const double LineSpacing = 1.35;
    public const double PadX = 10, PadY = 8;
}
