using BeDiagram.Models;

namespace BeDiagram.Cli;

/// <summary><c>check</c>: reports the problems <see cref="DiagramChecker"/> finds in each diagram. Changes nothing.</summary>
public class CheckCommand : DiagramFileCommand
{
    public override string Name
    {
        get
        {
            return "check";
        }
    }

    public override string Summary
    {
        get
        {
            return "Report broken references, connectors through shapes, items off the page, overlaps and cut-short rows";
        }
    }

    public override string Usage
    {
        get
        {
            return "<files or folders> [--quiet]   (--quiet: list only files with problems)";
        }
    }

    protected override Task<bool> RunOn(string path, Diagram d, CliArguments args, TextWriter output)
    {
        List<string> problems = new DiagramChecker(d).Check();
        if (problems.Count == 0)
        {
            if (!args.Has("quiet"))
            {
                output.WriteLine(path + ": OK");
            }
            return Task.FromResult(true);
        }
        output.WriteLine(path + ": " + problems.Count + " problem(s)");
        foreach (string p in problems)
        {
            output.WriteLine("  - " + p);
        }
        return Task.FromResult(false);
    }
}
