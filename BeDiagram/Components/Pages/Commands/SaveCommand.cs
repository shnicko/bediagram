namespace BeDiagram.Components.Pages.Commands;

/// <summary>Saves the diagram (asks for a name the first time).</summary>
public class SaveCommand : EditorCommand
{
    public SaveCommand() : base("Save", new KeyGesture("s", Ctrl: true)) { }

    public override Task ExecuteAsync(ICommandContext ctx) => ctx.SaveAsync();
}
