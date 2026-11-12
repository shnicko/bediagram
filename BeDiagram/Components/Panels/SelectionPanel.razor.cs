using BeDiagram.Components.Pages;
using BeDiagram.Models;

namespace BeDiagram.Components.Panels;

/// <summary>The side panel for several selected items: align, connector style between them, step order, swap, join, group, and bulk styling of the shapes, text boxes and groups in the selection.</summary>
public partial class SelectionPanel : DesignerPart
{
    /// <summary>Selected groups that are lanes (top-level); with two or more, Arrange works on just those.</summary>
    int SelectedLaneCount => Editor.TopLevelGroups().Count(g => Editor.Selection.Contains(g.Id));

    /// <summary>How many of the selected items are steps (shapes other than outcomes).</summary>
    int SelectedStepCount => D.Nodes.Count(n => Editor.Selection.Contains(n.Id) && n.Kind.IsStep);

    /// <summary>Every selected shape has a circle version (classic shapes), so *Draw as circle* can apply to them all.</summary>
    bool AllCanBeRound => SelectedNodes.All(n => n.Kind.CanBeRound);

    /// <summary>Every selected shape is already drawn as a circle: the checkbox shows as on.</summary>
    bool AllRound => SelectedNodes.All(n => n.Round);

    List<Node> SelectedNodes => D.Nodes.Where(n => Editor.Selection.Contains(n.Id)).ToList();

    List<TextBox> SelectedTextBoxes => D.TextBoxes.Where(t => Editor.Selection.Contains(t.Id)).ToList();

    List<Group> SelectedGroups => D.Groups.Where(g => Editor.Selection.Contains(g.Id)).ToList();
}
