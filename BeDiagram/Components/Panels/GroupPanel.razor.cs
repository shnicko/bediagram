using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Models;

namespace BeDiagram.Components.Panels;

/// <summary>The side panel for one selected group: title, look, members, link, alignment inside it, ungroup / delete and, for a top-level group, swim lanes.</summary>
public partial class GroupPanel : DesignerPart
{
    [Parameter, EditorRequired] public Group Group { get; set; } = default!;

    int MemberCount(Group g) => Editor.Members(g).Count();

    /// <summary>True for a group not inside another group (one that can be a swim lane).</summary>
    bool IsTopLevel(Group g) => Editor.Tree.Parent(g) is null;
}
