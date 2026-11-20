using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Models;
using BeDiagram.Rendering;
using BeDiagram.Services;

namespace BeDiagram.Components.Shared;

/// <summary>The clickable link badge on a shape or group (opens the linked diagram; Ctrl+click opens it in a new
/// tab). Rendered inside the canvas SVG.</summary>
public partial class LinkBadge
{
    [Inject] DiagramStore Store { get; set; } = default!;

    [Parameter, EditorRequired] public IBox Box { get; set; } = default!;
    [Parameter, EditorRequired] public string Colour { get; set; } = "";
    [Parameter, EditorRequired] public string Link { get; set; } = "";
    [Parameter] public EventCallback<string> OnFollow { get; set; }
    /// <summary>Ctrl+click (Cmd+click on a Mac): open the linked diagram in a new tab.</summary>
    [Parameter] public EventCallback<string> OnFollowNewTab { get; set; }

    bool Found
    {
        get
        {
            return Store.Exists(Link);
        }
    }

    string Tooltip
    {
        get
        {
            if (Found)
            {
                return $"More detail: open “{Link}” (Ctrl+click: in a new tab)";
            }
            return $"Linked diagram “{Link}” not found";
        }
    }

    MarkupString BadgeMarkup
    {
        get
        {
            return (MarkupString)SvgShapes.LinkBadgeSvg(Box, Colour);
        }
    }

    Task Click(MouseEventArgs e)
    {
        if ((e.CtrlKey || e.MetaKey) && OnFollowNewTab.HasDelegate)
        {
            return OnFollowNewTab.InvokeAsync(Link);
        }
        return OnFollow.InvokeAsync(Link);
    }
}
