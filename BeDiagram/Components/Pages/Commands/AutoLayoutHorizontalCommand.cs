using BeDiagram.Editing.Lanes;

namespace BeDiagram.Components.Pages.Commands;

/// <summary>Auto-layout with the flow running left → right.</summary>
public class AutoLayoutHorizontalCommand : AutoLayoutCommand
{
    public AutoLayoutHorizontalCommand() : base(LaneLayouts.Horizontal) { }
}
