using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Editing;
using BeDiagram.Models;

namespace BeDiagram.Components.Shared;

/// <summary>
/// W / H fields for several selected groups, applied together with one button (or Enter). A field shows the size when
/// every group has the same one, and is blank with "mixed" when they differ. What's typed is kept here until it's
/// applied; the multi-selection panel gives this component a new <c>@key</c> when the selected groups change, so
/// nothing typed carries over to a different selection.
/// </summary>
public partial class GroupSize
{
    [Parameter, EditorRequired] public DiagramEditor Editor { get; set; } = default!;

    [Parameter, EditorRequired] public IReadOnlyList<Group> Groups { get; set; } = default!;

    /// <summary>Called after the sizes are applied, so the page redraws the canvas.</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    /// <summary>What's typed in each field, or null when it hasn't been touched since the last apply.</summary>
    string? typedWidth;
    string? typedHeight;

    /// <summary>The width field: what's typed, otherwise the groups' shared width (blank when they differ).</summary>
    string WidthText
    {
        get
        {
            if (typedWidth is not null)
            {
                return typedWidth;
            }
            return Shared(true);
        }
    }

    string HeightText
    {
        get
        {
            if (typedHeight is not null)
            {
                return typedHeight;
            }
            return Shared(false);
        }
    }

    string WidthHint
    {
        get
        {
            return Hint(true);
        }
    }

    string HeightHint
    {
        get
        {
            return Hint(false);
        }
    }

    /// <summary>Apply is on once a field holds a number that differs from what's shown for the groups now.</summary>
    bool CanApply
    {
        get
        {
            return Typed(typedWidth, true) is not null || Typed(typedHeight, false) is not null;
        }
    }

    /// <summary>The button's text, naming how many groups it changes.</summary>
    string ApplyText
    {
        get
        {
            if (Groups.Count == 1)
            {
                return "Apply to 1 group";
            }
            return $"Apply to {Groups.Count} groups";
        }
    }

    /// <summary>The width (or height) every group has, in whole pixels, or blank when they differ.</summary>
    string Shared(bool width)
    {
        if (Groups.Count == 0)
        {
            return "";
        }
        double first = Size(Groups[0], width);
        foreach (Group g in Groups)
        {
            if (Math.Round(Size(g, width)) != Math.Round(first))
            {
                return "";
            }
        }
        return Math.Round(first).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>The placeholder: "mixed" when the groups' sizes differ.</summary>
    string Hint(bool width)
    {
        if (Shared(width) == "")
        {
            return "mixed";
        }
        return "";
    }

    static double Size(Group g, bool width)
    {
        if (width)
        {
            return g.W;
        }
        return g.H;
    }

    /// <summary>The number typed in a field, or null when the field is untouched, blank, not a number or the same as
    /// the groups' shared size.</summary>
    double? Typed(string? text, bool width)
    {
        if (text is null)
        {
            return null;
        }
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
        {
            return null;
        }
        if (text.Trim() == Shared(width))
        {
            return null;
        }
        return v;
    }

    void TypeWidth(ChangeEventArgs e)
    {
        typedWidth = e.Value?.ToString() ?? "";
    }

    void TypeHeight(ChangeEventArgs e)
    {
        typedHeight = e.Value?.ToString() ?? "";
    }

    /// <summary>Enter in either field applies, the same as the button.</summary>
    Task KeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            return Apply();
        }
        return Task.CompletedTask;
    }

    Task Apply()
    {
        double? w = Typed(typedWidth, true);
        double? h = Typed(typedHeight, false);
        if (w is null && h is null)
        {
            return Task.CompletedTask;
        }
        Editor.SetGroupsSize(Groups, w, h);
        // The fields go back to showing the groups' sizes (the typed one, or larger where the minimum applied).
        typedWidth = null;
        typedHeight = null;
        return OnChanged.InvokeAsync();
    }
}
