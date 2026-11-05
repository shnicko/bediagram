namespace BeDiagram.Models.Styles;

/// <summary>Group title placement and style keys.</summary>
public static class GroupLook
{
    public const string Top = "top";
    public const string Bottom = "bottom";
    public const string Band = "band";
    public const string Text = "text";

    public static readonly (string Key, string Name)[] Positions = { (Top, "Top"), (Bottom, "Bottom") };
    public static readonly (string Key, string Name)[] Styles = { (Band, "Band"), (Text, "Plain text") };

    /// <summary>
    /// Group outline width for a <see cref="LineLook"/> weight key. Regular stays at 1.5 (the width groups have always had),
    /// so existing diagrams look the same; light and heavy match connectors.
    /// </summary>
    public static double Width(string weight) => weight switch { LineLook.Light => 1, LineLook.Heavy => 3, _ => 1.5 };
}
