namespace BeDiagram.Models.Styles;

/// <summary>Group title placement and style keys.</summary>
public static class GroupLook
{
    public const string Top = "top";
    public const string Bottom = "bottom";
    public const string Band = "band";
    public const string Text = "text";
    /// <summary>The title in an outlined tag in the corner (a UML package or frame name).</summary>
    public const string Tag = "tag";

    public static readonly (string Key, string Name)[] Positions = { (Top, "Top"), (Bottom, "Bottom") };
    public static readonly (string Key, string Name)[] Styles = { (Band, "Band"), (Text, "Plain text"), (Tag, "Name tag") };

    /// <summary>
    /// Group outline width for a <see cref="LineLook"/> weight key. Regular stays at 1.5 (the width groups have always had),
    /// so existing diagrams look the same; light and heavy match connectors.
    /// </summary>
    public static double Width(string weight)
    {
        switch (weight)
        {
            case LineLook.Light:
                return 1;
            case LineLook.Heavy:
                return 3;
            default:
                return 1.5;
        }
    }
}
