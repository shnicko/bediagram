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
}
