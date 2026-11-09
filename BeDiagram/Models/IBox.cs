using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models;

public interface IBox
{
    string Id { get; }
    double X { get; set; }
    double Y { get; set; }
    double W { get; set; }
    double H { get; set; }

    /// <summary>The sides a connector end can be pinned to: all four, or left / right only on a row (<see cref="Layout.ItemBox"/>).</summary>
    IReadOnlyList<Side> ConnectSides => Sides.All;
}
