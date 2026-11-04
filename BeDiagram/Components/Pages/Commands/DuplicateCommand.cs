namespace BeDiagram.Components.Pages.Commands;

/// <summary>Copies the selection (with its connectors) next to the original.</summary>
public class DuplicateCommand : EditorCommand
{
    public DuplicateCommand() : base("Duplicate", new KeyGesture("d", Ctrl: true)) { }

    protected override void Execute(ICommandContext ctx) => ctx.Duplicate();
}
