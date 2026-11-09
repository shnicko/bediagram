using BeDiagram.Models;
using BeDiagram.Services;
using Microsoft.AspNetCore.Components;
using BeDiagram.Rendering;

namespace BeDiagram.Components.Shared;

/// <summary>The clickable link badge on a shape or group (opens the linked diagram). Rendered inside the canvas SVG.</summary>
public partial class LinkBadge
{
    [Inject] DiagramStore Store { get; set; } = default!;

    [Parameter, EditorRequired] public IBox Box { get; set; } = default!;
    [Parameter, EditorRequired] public string Colour { get; set; } = "";
    [Parameter, EditorRequired] public string Link { get; set; } = "";
    [Parameter] public EventCallback<string> OnFollow { get; set; }

    bool Found => Store.Exists(Link);

    string Tooltip => Found ? $"More detail: open “{Link}”" : $"Linked diagram “{Link}” not found";

    MarkupString BadgeMarkup => (MarkupString)SvgShapes.LinkBadgeSvg(Box, Colour);

    Task Click() => OnFollow.InvokeAsync(Link);
}
