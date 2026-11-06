using BeDiagram.Editing.Lanes;

namespace BeDiagram.Components.Pages.Commands;

/// <summary>
/// Lays the whole diagram out by step in one direction, then tidies the connectors (see DiagramEditor.AutoLayout).
/// No shortcut key: run from the Diagram panel. One subclass per direction so buttons can find each.
/// </summary>
public abstract class AutoLayoutCommand : EditorCommand
{
    readonly ILaneLayout layout;

    protected AutoLayoutCommand(ILaneLayout layout) : base($"Auto-layout {layout.Direction}")
    {
        this.layout = layout;
    }

    public override bool CanExecute(ICommandContext ctx) => ctx.HasShapes;

    protected override void Execute(ICommandContext ctx) => ctx.AutoLayout(layout);
}
