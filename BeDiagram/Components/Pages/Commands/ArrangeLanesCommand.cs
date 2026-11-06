using BeDiagram.Editing.Lanes;

namespace BeDiagram.Components.Pages.Commands;

/// <summary>
/// Arranges the top-level groups as swim lanes in one direction (see <see cref="LaneArranger"/>). No shortcut key:
/// run from the Diagram, group and multi-selection panels. One subclass per direction so buttons can find each.
/// </summary>
public abstract class ArrangeLanesCommand : EditorCommand
{
    readonly ILaneLayout layout;

    protected ArrangeLanesCommand(ILaneLayout layout) : base(layout.Name)
    {
        this.layout = layout;
    }

    public override bool CanExecute(ICommandContext ctx) => ctx.HasLanes;

    protected override void Execute(ICommandContext ctx) => ctx.ArrangeLanes(layout);
}
