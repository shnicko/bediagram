using BeDiagram.Editing;
using BeDiagram.Models;

namespace BeDiagram.Cli;

/// <summary>
/// <c>tidy</c>: runs Tidy connectors on every connector, exactly as Arrange → Tidy connectors does with nothing
/// selected, and saves the file when a route changed.
/// </summary>
public class TidyCommand : DiagramFileCommand
{
    public override string Name
    {
        get
        {
            return "tidy";
        }
    }

    public override string Summary
    {
        get
        {
            return "Tidy connectors in each diagram and save it";
        }
    }

    public override string Usage
    {
        get
        {
            return "<files or folders> [--dry-run]";
        }
    }

    protected override Task<bool> RunOn(string path, Diagram d, CliArguments args, TextWriter output)
    {
        ConsoleEditorView view = new ConsoleEditorView();
        DiagramEditor editor = new DiagramEditor(view);
        editor.Load(d);
        editor.TidyConnectors();
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
