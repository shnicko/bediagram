namespace BeDiagram.Components.Pages.Commands;

/// <summary>Switches the selected connector between elbow and straight.</summary>
public class ToggleConnectorStyleCommand : EditorCommand
{
    public ToggleConnectorStyleCommand() : base("Elbow / straight connector", new KeyGesture("l")) { }

    public override bool CanExecute(ICommandContext ctx) => ctx.HasSelectedConnector;

    protected override void Execute(ICommandContext ctx) => ctx.ToggleSelectedConnectorStyle();
}
