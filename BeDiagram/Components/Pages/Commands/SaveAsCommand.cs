namespace BeDiagram.Components.Pages.Commands;

/// <summary>Opens the Save as dialog.</summary>
public class SaveAsCommand : EditorCommand
{
    public SaveAsCommand() : base("Save as", new KeyGesture("s", Ctrl: true, Shift: true)) { }

    protected override void Execute(ICommandContext ctx) => ctx.ShowSaveAs();
}
