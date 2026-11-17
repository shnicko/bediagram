using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Models;
using BeDiagram.Models.Layout;

namespace BeDiagram.Components.Panels;

/// <summary>The side panel for one selected group: title, look, position and size, members, link, alignment inside it,
/// ungroup / delete and, for a top-level group, swim lanes.</summary>
public partial class GroupPanel : DesignerPart
{
    [Parameter, EditorRequired] public Group Group { get; set; } = default!;

    int MemberCount(Group g)
    {
        return Editor.Members(g).Count();
    }

    /// <summary>True for a group not inside another group (one that can be a swim lane).</summary>
    bool IsTopLevel(Group g)
    {
        return Editor.Tree.Parent(g) is null;
    }

    /// <summary>The position and size fields' values: the top-left corner and the size.</summary>
    Rect GroupBounds
    {
        get
        {
            return new Rect(Group.X, Group.Y, Group.W, Group.H);
        }
    }

    void SetBounds(Rect r)
    {
        Editor.SetGroupBounds(Group, r.X, r.Y, r.W, r.H);
    }
}
