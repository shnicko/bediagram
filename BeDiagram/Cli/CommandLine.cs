namespace BeDiagram.Cli;

/// <summary>
/// Runs BeDiagram as a command-line tool when the first argument is a command name, e.g.
/// <c>BeDiagram tidy "My diagrams"</c>, instead of starting the web app. Lists the commands for <c>help</c>.
/// </summary>
public class CommandLine
{
    readonly List<ICliCommand> commands = new List<ICliCommand>
    {
        new TidyCommand(),
        new LayoutCommand(),
        new ExportCommand(),
        new CheckCommand(),
        new McpCommand(),
    };

    /// <summary>
    /// True when the first argument is a word rather than an option, so the web app should not start: a command, help,
    /// or a mistyped command (which gets the help, not a web server started by mistake). Web app options such as
    /// <c>--urls</c> start with a dash and still start the app.
    /// </summary>
    public bool Handles(string[] args)
    {
        if (args.Length == 0)
        {
            return false;
        }
        string first = args[0];
        if (IsHelp(first))
        {
            return true;
        }
        return !first.StartsWith("-");
    }

    /// <summary>Runs the command named by the first argument and returns the process exit code.</summary>
    public async Task<int> RunAsync(string[] args, TextWriter output)
    {
        ICliCommand? command = Find(args[0]);
        if (command is null)
        {
            if (IsHelp(args[0]))
            {
                WriteHelp(output);
                return 0;
            }
            output.WriteLine("Unknown command: " + args[0]);
            output.WriteLine();
            WriteHelp(output);
            return 1;
        }
        CliArguments parsed = CliArguments.Parse(args);
        if (parsed.Has("help"))
        {
            output.WriteLine("BeDiagram " + command.Name + " " + command.Usage);
            output.WriteLine(command.Summary);
            return 0;
        }
        return await command.RunAsync(parsed, output);
    }

    ICliCommand? Find(string name)
    {
        foreach (ICliCommand c in commands)
        {
            if (string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return c;
            }
        }
        return null;
    }

    static bool IsHelp(string arg)
    {
        return arg == "help" || arg == "--help" || arg == "-h" || arg == "/?";
    }

    void WriteHelp(TextWriter output)
    {
        output.WriteLine("BeDiagram command line. With no command, BeDiagram starts the web app.");
        output.WriteLine();
        foreach (ICliCommand c in commands)
        {
            output.WriteLine("  " + c.Name.PadRight(8) + c.Summary);
            output.WriteLine("  " + "".PadRight(8) + c.Name + " " + c.Usage);
            output.WriteLine();
        }
        output.WriteLine("Folders are searched for *.diagram.json files, including subfolders.");
        output.WriteLine("The exit code is 1 when a file couldn't be read or check found problems.");
    }
}
