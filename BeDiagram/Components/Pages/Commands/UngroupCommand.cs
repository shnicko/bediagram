namespace BeDiagram.Components.Pages.Commands;

/// <summary>Removes the selected group, keeping its members.</summary>
public class UngroupCommand : EditorCommand
{
    public UngroupCommand() : base("Ungroup", new KeyGesture("g", Ctrl: true, Shift: true)) { }

    protected override void Execute(ICommandContext ctx) => ctx.UngroupSelected();
}
