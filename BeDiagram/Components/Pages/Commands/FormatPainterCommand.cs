namespace BeDiagram.Components.Pages.Commands;

/// <summary>
/// P: the format painter. Copies the look of the selected item (or uses the style already copied), then the next item
/// clicked gets that look. Shift+P (or double-clicking the brush) keeps painting until Esc. Pressing P again stops.
/// </summary>
public class FormatPainterCommand : EditorCommand
{
    public FormatPainterCommand(bool sticky)
        : base(NameFor(sticky), KeyFor(sticky))
    {
        Sticky = sticky;
    }

    /// <summary>Keeps painting until Esc (Shift+P), instead of stopping after one item.</summary>
    public bool Sticky { get; }

    static string NameFor(bool sticky)
    {
        if (sticky)
        {
            return "Format painter: keep painting until Esc";
        }
        return "Format painter: copy the selected item's look, click an item to paint it on";
    }

    static KeyGesture KeyFor(bool sticky)
    {
        if (sticky)
        {
            return new KeyGesture("p", Shift: true);
        }
        return new KeyGesture("p");
    }

    public override bool CanExecute(ICommandContext ctx)
    {
        return ctx.CanCopyStyle || ctx.HasCopiedStyle || ctx.Painting;
    }

    protected override void Execute(ICommandContext ctx)
    {
        ctx.StartPainter(Sticky);
    }
}
