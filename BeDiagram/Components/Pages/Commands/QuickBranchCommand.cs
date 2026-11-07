namespace BeDiagram.Components.Pages.Commands;

/// <summary>Branches the selected shape into Pass and Fail outcomes, each with a next step (see <c>DiagramEditor.QuickBranch</c>).</summary>
public class QuickBranchCommand : EditorCommand
{
    public QuickBranchCommand() : base("Branch: pass / fail", new KeyGesture("b")) { }

    public override bool CanExecute(ICommandContext ctx) => ctx.CanBranch;

    protected override void Execute(ICommandContext ctx) => ctx.QuickBranch();
}
