namespace BeDiagram.Components.Pages.Commands;

/// <summary>
/// Moves the text outside shapes (labels under circles and outcomes, descriptions) to the side with the fewest
/// connectors through it: the selected shapes, or all of them. A one-off clean-up run from the Arrange menu or the
/// shape panel; no shortcut key.
/// </summary>
public class PlaceTextCommand : EditorCommand
{
    public PlaceTextCommand() : base("Place text clear of connectors")
    {
    }

    public override bool CanExecute(ICommandContext ctx)
    {
        return ctx.HasOutsideText;
    }

    protected override void Execute(ICommandContext ctx)
    {
        ctx.PlaceTextClear();
    }
}
