using BeDiagram.Models.Layout;

namespace BeDiagram.Models.Location;

/// <summary>
/// Where an item is and how it moves and resizes (Strategy pattern). Every <see cref="IBox"/> has one, reached as
/// <c>b.Placement</c>: shapes measure from their centre (<see cref="ShapePlacement"/>, <see cref="PanelPlacement"/>),
/// text boxes and groups from their top-left corner (<see cref="CornerPlacement"/>, <see cref="GroupPlacement"/>).
/// The point X / Y are measured from is the item's <em>set-out point</em>; a new size keeps it where it is.
/// <see cref="Placement"/> holds the steps every item shares; the classes override only what differs.
/// </summary>
public interface IPlacement
{
    /// <summary>What X and Y measure, for the panel heading: "centre" or "top-left".</summary>
    string SetOutName { get; }

    /// <summary>The item's set-out point now.</summary>
    (double X, double Y) SetOut(IBox b);

    /// <summary>Where the top-left corner goes for a box of <paramref name="w"/> × <paramref name="h"/> whose set-out
    /// point is (<paramref name="setX"/>, <paramref name="setY"/>).</summary>
    (double X, double Y) TopLeftFor(double setX, double setY, double w, double h);

    /// <summary>The smallest width and height the item may have.</summary>
    (double W, double H) MinSize(IBox b);

    /// <summary>True when the height can't be set by hand (a list or IPO box is as tall as its rows).</summary>
    bool FixedHeight(IBox b);

    /// <summary>The values the position and size fields show: the set-out point and the size.</summary>
    Rect Fields(IBox b);

    /// <summary>The box (top-left corner and size) that typed <paramref name="fields"/> give: not below
    /// <see cref="MinSize"/>, keeping a fixed height. Null when it's the box the item already has.</summary>
    Rect? Plan(IBox b, Rect fields);

    /// <summary>Puts the item in <paramref name="box"/> (from <see cref="Plan"/>): moves it with <see cref="MoveTo"/>,
    /// then sizes it.</summary>
    void Apply(IBox b, Rect box, GroupTree tree, bool carry);

    /// <summary>A new size, keeping the set-out point. Not checked against <see cref="MinSize"/>: callers clamp.</summary>
    void Resize(IBox b, double w, double h);

    /// <summary>Moves the top-left corner to (<paramref name="x"/>, <paramref name="y"/>). A group also carries
    /// everything inside it when <paramref name="carry"/> is set.</summary>
    void MoveTo(IBox b, double x, double y, GroupTree tree, bool carry);
}
