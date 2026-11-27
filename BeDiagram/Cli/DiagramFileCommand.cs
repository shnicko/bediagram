using BeDiagram.Models;

namespace BeDiagram.Cli;

/// <summary>
/// The shared steps of a command that works through diagram files one at a time (Template Method): find the files
/// from the paths given, read each one, hand it to <see cref="RunOn"/>, and report how many had problems.
/// </summary>
public abstract class DiagramFileCommand : ICliCommand
{
    public abstract string Name { get; }

    public abstract string Summary { get; }

    public abstract string Usage { get; }

    public async Task<int> RunAsync(CliArguments args, TextWriter output)
    {
        if (args.Paths.Count == 0)
        {
            output.WriteLine("No diagram files given.");
            output.WriteLine("Usage: " + Name + " " + Usage);
            return 1;
        }
        List<string> missing = new List<string>();
        List<string> files = DiagramFile.Expand(args.Paths, missing);
        foreach (string m in missing)
        {
            output.WriteLine("Not found: " + m);
        }
        if (files.Count == 0)
        {
            output.WriteLine("No *.diagram.json files found.");
            return 1;
        }

        int failed = missing.Count;
        foreach (string path in files)
        {
            Diagram d;
            try
            {
                d = DiagramFile.Read(path);
            }
            catch (Exception ex)
            {
                output.WriteLine(path + ": can't read it (" + ex.Message + ")");
                failed++;
                continue;
            }
            bool ok = await RunOn(path, d, args, output);
            if (!ok)
            {
                failed++;
            }
        }
        if (files.Count > 1)
        {
            output.WriteLine(files.Count + " files, " + failed + " with problems.");
        }
        if (failed > 0)
        {
            return 1;
        }
        return 0;
    }

    /// <summary>Does the command's work on one diagram. Returns false when the file has problems or the work failed.</summary>
    protected abstract Task<bool> RunOn(string path, Diagram d, CliArguments args, TextWriter output);
}
