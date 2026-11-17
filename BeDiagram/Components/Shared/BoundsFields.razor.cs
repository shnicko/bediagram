using System.Globalization;
using Microsoft.AspNetCore.Components;
using BeDiagram.Models.Layout;

namespace BeDiagram.Components.Shared;

/// <summary>
/// The X / Y / W / H fields every item panel shows under "Position and size" (shapes, text boxes, groups), so they look
/// and behave the same. The panel decides what X and Y mean (a shape's centre, a box's top-left corner) and passes the
/// values in <see cref="Bounds"/>; a changed field comes back through <see cref="OnChange"/> with the other three
/// values as they were, and the panel hands them to the editor.
/// </summary>
public partial class BoundsFields
{
    /// <summary>The values shown: X and Y are the set-out point named by <see cref="SetOutPoint"/>.</summary>
    [Parameter, EditorRequired] public Rect Bounds { get; set; } = default!;

    /// <summary>What X and Y measure, shown next to the heading: "centre" or "top-left".</summary>
    [Parameter, EditorRequired] public string SetOutPoint { get; set; } = "";

    /// <summary>The smallest width and height the item may have (the editor enforces it; the fields show it).</summary>
    [Parameter] public double MinW { get; set; }

    [Parameter] public double MinH { get; set; }

    /// <summary>True when the height can't be typed (a list or IPO box is as tall as its rows).</summary>
    [Parameter] public bool FixedHeight { get; set; }

    /// <summary>The tooltip on the H field while <see cref="FixedHeight"/> is on.</summary>
    [Parameter] public string FixedHeightTip { get; set; } = "";

    /// <summary>The note behind the "i" button next to the heading.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Called with the new values after a field changes to a number.</summary>
    [Parameter] public EventCallback<Rect> OnChange { get; set; }

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

    Task SetX(ChangeEventArgs e)
    {
        double? v = Read(e);
        if (v is null)
        {
            return Task.CompletedTask;
        }
        return OnChange.InvokeAsync(new Rect(v.Value, Bounds.Y, Bounds.W, Bounds.H));
    }

    Task SetY(ChangeEventArgs e)
    {
        double? v = Read(e);
        if (v is null)
        {
            return Task.CompletedTask;
        }
        return OnChange.InvokeAsync(new Rect(Bounds.X, v.Value, Bounds.W, Bounds.H));
    }

    Task SetW(ChangeEventArgs e)
    {
        double? v = Read(e);
        if (v is null)
        {
            return Task.CompletedTask;
        }
        return OnChange.InvokeAsync(new Rect(Bounds.X, Bounds.Y, v.Value, Bounds.H));
    }

    Task SetH(ChangeEventArgs e)
    {
        double? v = Read(e);
        if (v is null)
        {
            return Task.CompletedTask;
        }
        return OnChange.InvokeAsync(new Rect(Bounds.X, Bounds.Y, Bounds.W, v.Value));
    }
}
