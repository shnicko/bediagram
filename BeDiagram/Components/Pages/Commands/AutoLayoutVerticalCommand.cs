using BeDiagram.Editing.Lanes;

namespace BeDiagram.Components.Pages.Commands;

/// <summary>Auto-layout with the flow running top → down.</summary>
public class AutoLayoutVerticalCommand : AutoLayoutCommand
{
    public AutoLayoutVerticalCommand() : base(LaneLayouts.Vertical) { }
}
