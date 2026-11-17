using System.Globalization;
using Microsoft.AspNetCore.Components;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Location;

namespace BeDiagram.Components.Shared;

/// <summary>
/// The X / Y / W / H fields every item panel shows under "Position and size" (shapes, text boxes, groups), so they look
/// and behave the same. Everything about where the item is measured from comes from its <see cref="IPlacement"/>: the
/// set-out point named in the heading and shown in X / Y, the smallest size, and whether the height can be typed. A
/// changed field goes to <see cref="DiagramEditor.SetBounds"/> with the other three values as they were.
/// </summary>
public partial class BoundsFields
{
    [Parameter, EditorRequired] public DiagramEditor Editor { get; set; } = default!;

    /// <summary>The shape, text box or group the fields are for.</summary>
    [Parameter, EditorRequired] public IGroupable Item { get; set; } = default!;

    /// <summary>The tooltip on the H field when the height can't be typed.</summary>
    [Parameter] public string FixedHeightTip { get; set; } = "";

    /// <summary>The note behind the "i" button next to the heading.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Called after every change, so the page redraws the canvas (a change here only re-renders this component).</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    IPlacement Placement
    {
        get
        {
            return Item.Placement;
        }
    }

    /// <summary>The values shown: the set-out point and the size.</summary>
    Rect Bounds
    {
        get
        {
            return Placement.Fields(Item);
        }
    }

    /// <summary>What X and Y measure, shown next to the heading: "centre", "top centre" or "top-left".</summary>
    string SetOutPoint
    {
        get
        {
            return Placement.SetOutName;
        }
    }

    double MinW
    {
        get
        {
            return Placement.MinSize(Item).W;
        }
    }

    double MinH
    {
        get
        {
            return Placement.MinSize(Item).H;
        }
    }

    bool FixedHeight
    {
        get
        {
            return Placement.FixedHeight(Item);
        }
    }

    string HeightTitle
    {
        get
        {
            if (FixedHeight)
            {
                return FixedHeightTip;
            }
            return "";
        }
    }

    /// <summary>A position or size for the number fields: at most one decimal, so a half-pixel centre shows as it is.</summary>
    static string Num(double v)
    {
        return Math.Round(v, 1).ToString("0.#", CultureInfo.InvariantCulture);
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

    /// <summary>Hands the typed values to the editor and redraws the page.</summary>
    Task Set(Rect fields)
    {
        Editor.SetBounds(Item, fields);
        return OnChanged.InvokeAsync();
    }

    Task SetX(ChangeEventArgs e)
    {
        double? v = Read(e);
        if (v is null)
        {
            return Task.CompletedTask;
        }
        Rect now = Bounds;
        return Set(new Rect(v.Value, now.Y, now.W, now.H));
    }

    Task SetY(ChangeEventArgs e)
    {
        double? v = Read(e);
        if (v is null)
        {
            return Task.CompletedTask;
        }
        Rect now = Bounds;
        return Set(new Rect(now.X, v.Value, now.W, now.H));
    }

    Task SetW(ChangeEventArgs e)
    {
        double? v = Read(e);
        if (v is null)
        {
            return Task.CompletedTask;
        }
        Rect now = Bounds;
        return Set(new Rect(now.X, now.Y, v.Value, now.H));
    }

    Task SetH(ChangeEventArgs e)
    {
        double? v = Read(e);
        if (v is null)
        {
            return Task.CompletedTask;
        }
        Rect now = Bounds;
        return Set(new Rect(now.X, now.Y, now.W, v.Value));
    }
}
