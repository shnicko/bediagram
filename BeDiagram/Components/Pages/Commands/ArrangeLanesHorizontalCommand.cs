using BeDiagram.Editing.Lanes;

namespace BeDiagram.Components.Pages.Commands;

/// <summary>Lanes stacked as rows, the flow running left → right.</summary>
public class ArrangeLanesHorizontalCommand : ArrangeLanesCommand
{
    public ArrangeLanesHorizontalCommand() : base(LaneLayouts.Horizontal) { }
}
