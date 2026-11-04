using Microsoft.JSInterop;
using BeDiagram.Editing;

namespace BeDiagram.Components.Pages;

/// <summary>The Designer as the screen the <see cref="DiagramEditor"/> talks back to (<see cref="IEditorView"/>).</summary>
public partial class Designer : IEditorView
{
    void IEditorView.ShowToast(string message) => ShowToast(message);

    (double X, double Y) IEditorView.ViewCentre() => ViewCentre();

    void IEditorView.EditStarted()
    {
        CancelGesture();
        _ = module?.InvokeVoidAsync("focus", "edit-box");
    }
}
