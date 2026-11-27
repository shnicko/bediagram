namespace BeDiagram.Cli;

/// <summary>
/// One command-line command (Command pattern, like the editor's <c>IEditorCommand</c>): <c>tidy</c>, <c>layout</c>,
/// <c>export</c>, <c>check</c>. Each works on diagram files directly, without starting the web app. To add one, write a
/// class that implements this interface and list it in <see cref="CommandLine"/>.
/// </summary>
public interface ICliCommand
{
    /// <summary>The word typed after the program name, e.g. "tidy".</summary>
    string Name { get; }

    /// <summary>One line for the help list.</summary>
    string Summary { get; }

    /// <summary>The arguments it takes, e.g. "&lt;files or folders&gt; [--svg] [--pdf]".</summary>
    string Usage { get; }

    /// <summary>Runs the command and returns the exit code: 0 when it worked, 1 when it found problems or failed.</summary>
    Task<int> RunAsync(CliArguments args, TextWriter output);
}
