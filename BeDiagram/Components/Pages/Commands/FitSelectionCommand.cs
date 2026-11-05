namespace BeDiagram.Components.Pages.Commands;

/// <summary>Zooms and centres on the selected items (or the two ends of the selected connector).</summary>
public class FitSelectionCommand : EditorCommand
{
    public FitSelectionCommand() : base("Zoom to selection", new KeyGesture("f", Shift: true)) { }

    public override bool CanExecute(ICommandContext ctx) => ctx.HasSelection;

    protected override void Execute(ICommandContext ctx) => ctx.FitSelection();
}
