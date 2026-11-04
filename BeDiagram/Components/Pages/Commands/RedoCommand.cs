namespace BeDiagram.Components.Pages.Commands;

/// <summary>Re-applies an undone change.</summary>
public class RedoCommand : EditorCommand
{
    public RedoCommand() : base("Redo", new KeyGesture("y", Ctrl: true), new KeyGesture("z", Ctrl: true, Shift: true)) { }

    public override bool CanExecute(ICommandContext ctx) => ctx.CanRedo;

    protected override void Execute(ICommandContext ctx) => ctx.Redo();
}
