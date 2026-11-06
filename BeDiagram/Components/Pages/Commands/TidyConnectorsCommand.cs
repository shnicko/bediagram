namespace BeDiagram.Components.Pages.Commands;

/// <summary>
/// Re-routes connectors that pass through shapes (the selected connector, those on the selection, or all of them).
/// A one-off clean-up run from the toolbar; no shortcut key.
/// </summary>
public class TidyConnectorsCommand : EditorCommand
{
    public TidyConnectorsCommand() : base("Tidy connectors") { }

    public override bool CanExecute(ICommandContext ctx) => ctx.HasConnectors;

    protected override void Execute(ICommandContext ctx) => ctx.TidyConnectors();
}
