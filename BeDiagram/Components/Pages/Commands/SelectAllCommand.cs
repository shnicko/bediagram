namespace BeDiagram.Components.Pages.Commands;

/// <summary>Selects every shape, text box and group.</summary>
public class SelectAllCommand : EditorCommand
{
    public SelectAllCommand() : base("Select all", new KeyGesture("a", Ctrl: true)) { }

    protected override void Execute(ICommandContext ctx) => ctx.SelectAll();
}
