using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Location;

namespace BeDiagram.Models;

public interface IBox
{
    string Id { get; }
    double X { get; set; }
    double Y { get; set; }
    double W { get; set; }
    double H { get; set; }

    /// <summary>The sides a connector end can be pinned to: all four, or left / right only on a row (<see cref="Layout.ItemBox"/>).</summary>
    IReadOnlyList<Side> ConnectSides
    {
        get
        {
            return Sides.All;
        }
    }

    /// <summary>Where the item is measured from and how it moves and resizes (<see cref="IPlacement"/>). Not saved.</summary>
    IPlacement Placement { get; }
}
