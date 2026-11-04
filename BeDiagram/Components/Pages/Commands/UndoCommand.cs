namespace BeDiagram.Components.Pages.Commands;

/// <summary>Steps back to the previous undo snapshot.</summary>
public class UndoCommand : EditorCommand
{
    public UndoCommand() : base("Undo", new KeyGesture("z", Ctrl: true)) { }

    public override bool CanExecute(ICommandContext ctx) => ctx.CanUndo;

    protected override void Execute(ICommandContext ctx) => ctx.Undo();
}
