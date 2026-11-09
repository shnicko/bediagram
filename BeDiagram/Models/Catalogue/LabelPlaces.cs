namespace BeDiagram.Models.Catalogue;

/// <summary>The connector label places, in panel order, with their button text.</summary>
public static class LabelPlaces
{
    public static readonly (LabelPlace Place, string Name)[] All =
        { (LabelPlace.Inline, "Inline"), (LabelPlace.Above, "Above"), (LabelPlace.Below, "Below") };
}
