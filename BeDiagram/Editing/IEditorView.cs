namespace BeDiagram.Editing;

/// <summary>
/// What <see cref="DiagramEditor"/> needs from the screen showing it. The Designer page implements it; the editor
/// itself has no Blazor, browser or file code, so it can be used (and tested) without a UI.
/// </summary>
public interface IEditorView
{
    /// <summary>Shows a short message in the status bar.</summary>
    void ShowToast(string message);

    /// <summary>The middle of the visible canvas, in world coordinates (where quick-added items go).</summary>
    (double X, double Y) ViewCentre();

    /// <summary>Inline text editing started (<see cref="DiagramEditor.EditingId"/> is set): stop any drag and focus the editor box.</summary>
    void EditStarted();
}
