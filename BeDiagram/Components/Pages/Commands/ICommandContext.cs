using BeDiagram.Editing.Lanes;
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
    /// <summary>Any shape, text box, group or connector is selected.</summary>
    bool HasSelection { get; }

    // ───────────── Tabs ─────────────

    /// <summary>How many diagrams are open.</summary>
    int TabCount { get; }
    /// <summary>Shows the tab at <paramref name="index"/> (0 = the first).</summary>
    void GoToTab(int index);
    /// <summary>Shows the next (+1) or previous (−1) tab, wrapping round at the ends.</summary>
    void StepTab(int delta);
    /// <summary>Closes the tab on screen, asking first when it has unsaved changes.</summary>
    void CloseActiveTab();
    /// <summary>A new, empty diagram in a new tab.</summary>
    void NewDiagram();

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
    /// <summary>Exactly one (non-outcome) shape is selected, so a quick branch can start from it.</summary>
    bool CanBranch { get; }
    void QuickBranch();
    void Nudge(double dx, double dy);
    void ToggleSelectedConnectorStyle();
    /// <summary>Switches the angle lock between Free and the last lock picked.</summary>
    void ToggleAngleLock();

    /// <summary>The diagram has shapes or text boxes to lay out.</summary>
    bool HasShapes { get; }
    /// <summary>Lays the whole diagram out by step in one direction, then tidies the connectors.</summary>
    void AutoLayout(ILaneLayout layout);

    /// <summary>The diagram has at least one connector.</summary>
    bool HasConnectors { get; }
    /// <summary>Re-routes connectors that pass through shapes (selected connector, those on the selection, or all).</summary>
    void TidyConnectors();

    /// <summary>Some shape has text outside it (a label under a circle or outcome, a description).</summary>
    bool HasOutsideText { get; }
    /// <summary>Moves the text outside shapes clear of connectors (the selected shapes, or all).</summary>
    void PlaceTextClear();

    /// <summary>There is at least one top-level group to arrange as a swim lane.</summary>
    bool HasLanes { get; }
    /// <summary>Arranges the top-level groups (or the selected ones) as swim lanes.</summary>
    void ArrangeLanes(ILaneLayout layout);

    /// <summary>Adds a shape of <paramref name="type"/> (see <see cref="NodeTypes"/>) below the selection.</summary>
    void AddShape(NodeType type);
    void AddText();
    void AddComment();
    void AddGroup();

    void Fit();
    void FitSelection();
    void ZoomBy(double factor);
    void ResetZoom();
}
