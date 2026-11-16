using System.Globalization;
using Microsoft.AspNetCore.Components;
using BeDiagram.Components.Pages;
using BeDiagram.Models;

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

    /// <summary>A position or size for the number fields: whole pixels.</summary>
    static string Num(double v)
    {
        return Math.Round(v).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>The number typed into a field, or null when it isn't one.</summary>
    static double? Read(ChangeEventArgs e)
    {
        string text = e.Value?.ToString() ?? "";
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
        {
            return v;
        }
        return null;
    }

    void SetX(ChangeEventArgs e)
    {
        double? v = Read(e);
        if (v is not null)
        {
            Editor.SetGroupBounds(Group, v.Value, Group.Y, Group.W, Group.H);
        }
    }

    void SetY(ChangeEventArgs e)
    {
        double? v = Read(e);
        if (v is not null)
        {
            Editor.SetGroupBounds(Group, Group.X, v.Value, Group.W, Group.H);
        }
    }

    void SetW(ChangeEventArgs e)
    {
        double? v = Read(e);
        if (v is not null)
        {
            Editor.SetGroupBounds(Group, Group.X, Group.Y, v.Value, Group.H);
        }
    }

    void SetH(ChangeEventArgs e)
    {
        double? v = Read(e);
        if (v is not null)
        {
            Editor.SetGroupBounds(Group, Group.X, Group.Y, Group.W, v.Value);
        }
    }
}
