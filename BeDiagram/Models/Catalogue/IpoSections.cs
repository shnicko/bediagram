namespace BeDiagram.Models.Catalogue;

/// <summary>The three sections of an input–process–output box, in drawing order.</summary>
public static class IpoSections
{
    public const string Input = "input", Process = "process", Output = "output";

    public static readonly IReadOnlyList<string> All = new[] { Input, Process, Output };

    /// <summary>The title a section shows until it's renamed (<see cref="Node.SectionTitles"/>).</summary>
    public static string DefaultTitle(string section) => section switch
    {
        Process => "Process",
        Output => "Output",
        _ => "Input",
    };

    /// <summary>A stored section key, or <see cref="Input"/> for anything unknown (a row copied from a list).</summary>
    public static string Of(string? section) => section is Process or Output ? section : Input;

    public static int IndexOf(string? section) => Of(section) switch { Process => 1, Output => 2, _ => 0 };
}
