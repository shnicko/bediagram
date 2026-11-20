namespace BeDiagram.Components.Pages.Commands;

/// <summary>Alt+1 to Alt+8 show that tab; Alt+9 shows the last tab, as in a web browser.</summary>
public class GoToTabCommand : EditorCommand
{
    /// <summary>The key, 1–9.</summary>
    readonly int number;

    public GoToTabCommand(int number)
        : base(NameFor(number), new KeyGesture(number.ToString(), Alt: true))
    {
        this.number = number;
    }

    static string NameFor(int number)
    {
        if (number == 9)
        {
            return "Show the last tab";
        }
        return $"Show tab {number}";
    }

    public override bool CanExecute(ICommandContext ctx)
    {
        return ctx.TabCount > 1;
    }

    protected override void Execute(ICommandContext ctx)
    {
        if (number == 9)
        {
            ctx.GoToTab(ctx.TabCount - 1);
            return;
        }
        if (number <= ctx.TabCount)
        {
            ctx.GoToTab(number - 1);
        }
    }
}
