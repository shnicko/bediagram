using BeDiagram.Editing;

namespace BeDiagram.Cli;

/// <summary>
/// The screen for a <see cref="DiagramEditor"/> run from the command line: the status-bar messages it would show are
/// kept so the command can print them, and there is no canvas to pan or focus.
/// </summary>
public class ConsoleEditorView : IEditorView
{
    /// <summary>The messages the editor showed, oldest first.</summary>
    public List<string> Messages { get; } = new List<string>();

    public void ShowToast(string message)
    {
        Messages.Add(message);
    }

    /// <summary>Where the editor treats the middle of the view as being: where a shape added with nothing selected
    /// goes. (0, 0) unless the caller picks a spot (the MCP server's add_shape puts it below the drawing).</summary>
    public (double X, double Y) Centre { get; set; } = (0, 0);

    public (double X, double Y) ViewCentre()
    {
        return Centre;
    }

    public void CentreOn(double x, double y)
    {
    }

    public void EditStarted()
    {
    }
}
