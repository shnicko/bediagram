using BeDiagram.Editing;
using BeDiagram.Editing.Lanes;
using BeDiagram.Models;

namespace BeDiagram.Cli;

/// <summary>
/// <c>layout</c>: Auto-layout (or, with <c>--lanes</c>, Arrange as swim lanes) in the diagram's flow direction, or the
/// one asked for, then saves. <c>--tidy</c> also tidies the connectors after arranging lanes (auto-layout always does).
/// </summary>
public class LayoutCommand : DiagramFileCommand
{
    public override string Name
    {
        get
        {
            return "layout";
        }
    }

    public override string Summary
    {
        get
        {
            return "Auto-layout by step (or arrange swim lanes) and save";
        }
    }

    public override string Usage
    {
        get
        {
            return "<files or folders> [--right | --down] [--lanes] [--tidy] [--dry-run]";
        }
    }

    protected override Task<bool> RunOn(string path, Diagram d, CliArguments args, TextWriter output)
    {
        // The direction asked for, otherwise the diagram's own flow direction.
        ILaneLayout layout = LaneLayouts.ForFlow(d.Flow);
        if (args.Has("right"))
        {
            layout = LaneLayouts.Horizontal;
        }
        else if (args.Has("down"))
        {
            layout = LaneLayouts.Vertical;
        }

        ConsoleEditorView view = new ConsoleEditorView();
        DiagramEditor editor = new DiagramEditor(view);
        editor.Load(d);
        if (args.Has("lanes"))
        {
            editor.ArrangeLanes(layout);
            if (args.Has("tidy"))
            {
                editor.TidyConnectors();
            }
        }
        else
        {
            editor.AutoLayout(layout);
        }

        string saved = "";
        if (editor.Dirty && !args.Has("dry-run"))
        {
            DiagramFile.Write(path, editor.Diagram);
            saved = " (saved)";
        }
        output.WriteLine(path + ": " + string.Join("; ", view.Messages) + saved);
        return Task.FromResult(true);
    }
}
