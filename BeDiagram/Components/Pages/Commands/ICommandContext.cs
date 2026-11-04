using BeDiagram.Models.Catalogue;

namespace BeDiagram.Components.Pages.Commands;

/// <summary>
/// What commands may ask the editor to do (the receiver in the Command pattern). The Designer implements it in
/// Designer.CommandContext.cs. If a new command needs something else, add it here and there.
/// </summary>
public interface ICommandContext
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    bool HasSelectedConnector { get; }

    Task SaveAsync();
    void ShowSaveAs();
    void Undo();
    void Redo();

    void SelectAll();
    /// <summary>Escape: stop editing and any drag, clear the selection.</summary>
    void CancelAndClearSelection();
    /// <summary>Enter: edit the text of the selected item or connector.</summary>
    void EditSelected();

    void Duplicate();
    void DeleteSelection();
    void GroupSelection();
    void UngroupSelected();
    void JoinSelected();
    void Nudge(double dx, double dy);
    void ToggleSelectedConnectorStyle();

    /// <summary>Adds a shape of <paramref name="type"/> (see <see cref="NodeTypes"/>) below the selection.</summary>
    void AddShape(string type);
    void AddText();
    void AddComment();
    void AddGroup();

    void Fit();
    void ZoomBy(double factor);
    void ResetZoom();
}
