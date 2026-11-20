namespace BeDiagram.Components.Pages.Commands;

/// <summary>Ctrl+Alt+Shift+V: paste the copied look plus the parts picked in the Paste special pop-up (size, type,
/// text, properties, link).</summary>
public class PasteSpecialCommand : EditorCommand
{
    public PasteSpecialCommand() : base("Paste special…", new KeyGesture("v", Ctrl: true, Shift: true, Alt: true))
    {
    }

    public override bool CanExecute(ICommandContext ctx)
    {
        return ctx.HasCopiedStyle && ctx.HasSelection;
    }

    protected override void Execute(ICommandContext ctx)
    {
        ctx.ShowPasteSpecial();
    }
}
