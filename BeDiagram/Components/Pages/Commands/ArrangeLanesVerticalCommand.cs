using BeDiagram.Editing.Lanes;

namespace BeDiagram.Components.Pages.Commands;

/// <summary>Lanes side by side as columns, the flow running top → down.</summary>
public class ArrangeLanesVerticalCommand : ArrangeLanesCommand
{
    public ArrangeLanesVerticalCommand() : base(LaneLayouts.Vertical) { }
}
