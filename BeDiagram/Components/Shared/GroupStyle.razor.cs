using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Styles;
using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Shared;

/// <summary>
/// A group's look: colour, tint, border pattern and weight, title position and style. Used by the group panel (one
/// group) and the multi-selection panel (every selected group), restyling them all in one undo step. A button shows
/// as on only when every group has that setting.
/// </summary>
public partial class GroupStyle
{
    [Parameter, EditorRequired] public DiagramEditor Editor { get; set; } = default!;

    [Parameter, EditorRequired] public IReadOnlyList<Group> Groups { get; set; } = default!;

    /// <summary>Called after every change, so the page redraws the canvas (a click here only re-renders this component).</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    Task Set(Action<Group> change) { Editor.Restyle(Groups, change); return OnChanged.InvokeAsync(); }

    Task SetTitleLook(string? pos, string? style) { Editor.SetGroupTitleLook(Groups, pos, style); return OnChanged.InvokeAsync(); }

    string On(Func<Group, bool> test) => Groups.Count > 0 && Groups.All(test) ? "on" : "";

    bool AllFilled => Groups.All(g => g.Filled);

    /// <summary>Weight the border pattern samples are drawn at: the groups' own when they agree.</summary>
    string SampleWeight => Groups.Select(g => g.BorderWeight).Distinct().Count() == 1 ? Groups[0].BorderWeight : LineLook.Regular;

    static string StyleTip(string style) => style == GroupLook.Band
        ? "Title on a tinted strip inside the group"
        : "Plain bold title — inside at the top, or centred under the box when the title is at the bottom";
}
