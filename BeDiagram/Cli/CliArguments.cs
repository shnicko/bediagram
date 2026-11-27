namespace BeDiagram.Cli;

/// <summary>
/// The arguments after the command word: diagram paths, <c>--flags</c>, and <c>--name value</c> options
/// (only the names in <see cref="ValueOptions"/> take a value).
/// </summary>
public class CliArguments
{
    /// <summary>Options that are followed by a value.</summary>
    static readonly HashSet<string> ValueOptions = new HashSet<string> { "out", "folder" };

    /// <summary>The paths given (files or folders), in order.</summary>
    public List<string> Paths { get; } = new List<string>();

    /// <summary>The flags given, without the leading dashes, e.g. "pdf".</summary>
    public HashSet<string> Flags { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>The options given with a value, e.g. "out" → "exports".</summary>
    public Dictionary<string, string> Options { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Reads the arguments after the command word (<paramref name="args"/>[0] is the command and is skipped).</summary>
    public static CliArguments Parse(string[] args)
    {
        CliArguments result = new CliArguments();
        for (int i = 1; i < args.Length; i++)
        {
            string a = args[i];
            if (a.StartsWith("--"))
            {
                string name = a.Substring(2);
                if (ValueOptions.Contains(name) && i + 1 < args.Length)
                {
                    result.Options[name] = args[i + 1];
                    i++;
                }
                else
                {
                    result.Flags.Add(name);
                }
            }
            else
            {
                result.Paths.Add(a);
            }
        }
        return result;
    }

    /// <summary>True when the flag was given (<c>--name</c>).</summary>
    public bool Has(string flag)
    {
        return Flags.Contains(flag);
    }

    /// <summary>The option's value, or null when it wasn't given.</summary>
    public string? Option(string name)
    {
        string? value;
        if (Options.TryGetValue(name, out value))
        {
            return value;
        }
        return null;
    }
}
