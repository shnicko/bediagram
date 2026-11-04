namespace BeDiagram.Models.Catalogue;

public static class LabelPlaces
{
    public const string Inline = "inline";
    public const string Above = "above";
    public const string Below = "below";
    public static readonly (string Key, string Name)[] All = { (Inline, "Inline"), (Above, "Above"), (Below, "Below") };
}
