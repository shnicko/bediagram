using BeDiagram.Components.Pages;
using BeDiagram.Models;

namespace BeDiagram.Components.Panels;

/// <summary>The side panel for several selected items: align, connector style between them, step order, swap, join, group, and bulk styling of the shapes, text boxes and groups in the selection.</summary>
public partial class SelectionPanel : DesignerPart
{
    /// <summary>Selected groups that are lanes (top-level); with two or more, Arrange works on just those.</summary>
    int SelectedLaneCount
    {
        get
        {
            return Editor.TopLevelGroups().Count(g => Editor.Selection.Contains(g.Id));
        }
    }

    /// <summary>How many of the selected items are steps (shapes other than outcomes).</summary>
    int SelectedStepCount
    {
        get
        {
            return D.Nodes.Count(n => Editor.Selection.Contains(n.Id) && n.Kind.IsStep);
        }
    }

    /// <summary>Every selected shape has a circle version (classic shapes), so *Draw as circle* can apply to them all.</summary>
    bool AllCanBeRound
    {
        get
        {
            return SelectedNodes.All(n => n.Kind.CanBeRound);
        }
    }

    /// <summary>Every selected shape is already drawn as a circle: the checkbox shows as on.</summary>
    bool AllRound
    {
        get
        {
            return SelectedNodes.All(n => n.Round);
        }
    }

    List<Node> SelectedNodes
    {
        get
        {
            List<Node> result = new List<Node>();
            foreach (Node n in D.Nodes)
            {
                if (Editor.Selection.Contains(n.Id))
                {
                    result.Add(n);
                }
            }
            return result;
        }
    }

    List<TextBox> SelectedTextBoxes
    {
        get
        {
            List<TextBox> result = new List<TextBox>();
            foreach (TextBox t in D.TextBoxes)
            {
                if (Editor.Selection.Contains(t.Id))
                {
                    result.Add(t);
                }
            }
            return result;
        }
    }

    List<Group> SelectedGroups
    {
        get
        {
            List<Group> result = new List<Group>();
            foreach (Group g in D.Groups)
            {
                if (Editor.Selection.Contains(g.Id))
                {
                    result.Add(g);
                }
            }
            return result;
        }
    }

    /// <summary>The selected groups' ids in one string: the size fields start afresh when it changes, so a width typed
    /// for one set of groups isn't applied to another.</summary>
    string SelectedGroupKey
    {
        get
        {
            List<string> ids = new List<string>();
            foreach (Group g in SelectedGroups)
            {
                ids.Add(g.Id);
            }
            return string.Join("|", ids);
        }
    }
}
