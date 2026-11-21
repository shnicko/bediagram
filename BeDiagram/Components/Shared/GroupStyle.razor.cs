using Microsoft.AspNetCore.Components;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Presets;
using BeDiagram.Models.Styles;
using BeDiagram.Services;

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

    Task Set(Action<Group> change)
    {
        Editor.Restyle(Groups, change);
        return OnChanged.InvokeAsync();
    }

    Task SetTitleLook(string? pos, string? style)
    {
        Editor.SetGroupTitleLook(Groups, pos, style);
        return OnChanged.InvokeAsync();
    }

    /// <summary>"on" when there are groups and every one passes <paramref name="test"/>.</summary>
    string On(Func<Group, bool> test)
    {
        if (Groups.Count == 0)
        {
            return "";
        }
        foreach (Group g in Groups)
        {
            if (!test(g))
            {
                return "";
            }
        }
        return "on";
    }

    bool AllFilled
    {
        get
        {
            foreach (Group g in Groups)
            {
                if (!g.Filled)
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>Weight the border pattern samples are drawn at: the groups' own when they agree.</summary>
    string SampleWeight
    {
        get
        {
            if (Groups.Count == 0)
            {
                return LineLook.Regular;
            }
            foreach (Group g in Groups)
            {
                if (g.BorderWeight != Groups[0].BorderWeight)
                {
                    return LineLook.Regular;
                }
            }
            return Groups[0].BorderWeight;
        }
    }

    static string StyleTip(string style)
    {
        switch (style)
        {
            case GroupLook.Band:
                return "Title on a tinted strip inside the group";
            case GroupLook.Tag:
                return "Title in an outlined name tag in the corner, as for a UML package";
            default:
                return "Plain bold title — inside at the top, or centred under the box when the title is at the bottom";
        }
    }

    // ───────────── Saved styles ─────────────

    [Inject] StylePresetStore Store { get; set; } = default!;

    /// <summary>The saved group styles.</summary>
    List<StylePreset> SavedStyles
    {
        get
        {
            return Store.Groups();
        }
    }

    /// <summary>Saving needs exactly one group; null hides the save row.</summary>
    Func<PresetText, StylePreset>? Capture
    {
        get
        {
            if (Groups.Count != 1)
            {
                return null;
            }
            return CaptureOne;
        }
    }

    StylePreset CaptureOne(PresetText text)
    {
        StylePreset p = new StylePreset();
        p.Group = GroupPreset.From(Groups[0], text);
        return p;
    }

    Task ApplyPreset(StylePreset p)
    {
        if (p.Group is not null)
        {
            Editor.ApplyPreset(Groups, p.Group);
        }
        return OnChanged.InvokeAsync();
    }
}
