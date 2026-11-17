namespace BeDiagram.Models.Location;

/// <summary>A list or IPO box: placed by its centre like any shape, but always as tall as its rows, so its height
/// can't be set by hand.</summary>
public class PanelPlacement : ShapePlacement
{
    public override bool FixedHeight(IBox b)
    {
        return true;
    }
}
