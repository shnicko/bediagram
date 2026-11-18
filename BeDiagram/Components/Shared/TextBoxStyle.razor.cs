using Microsoft.AspNetCore.Components;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Styles;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Components.Shared;

/// <summary>
/// A text box's look: font, size, bold, alignment, text colour, background, border and corners. Used by the text box
/// panel (one box) and the multi-selection panel (every selected box), restyling them all in one undo step. A button
/// shows as on only when every box has that setting; mixed font sizes show "—".
/// </summary>
public partial class TextBoxStyle
{
    [Parameter, EditorRequired] public DiagramEditor Editor { get; set; } = default!;

    [Parameter, EditorRequired] public IReadOnlyList<TextBox> Boxes { get; set; } = default!;

    /// <summary>Called after every change, so the page redraws the canvas (a click here only re-renders this component).</summary>
    [Parameter] public EventCallback OnChanged { get; set; }

    Task Set(Action<TextBox> change)
    {
        Editor.Restyle(Boxes, change);
        return OnChanged.InvokeAsync();
    }

    Task SetFontSize(object? value)
    {
        Editor.SetFontSize(Boxes, value);
        return OnChanged.InvokeAsync();
    }

    static readonly (string Key, string Name)[] HAligns = { (TextLook.Left, "Left"), (TextLook.Center, "Centre"), (TextLook.Right, "Right") };
    static readonly (string Key, string Name)[] VAligns = { (TextLook.Top, "Top"), (TextLook.Middle, "Middle"), (TextLook.Bottom, "Bottom") };

    /// <summary>"on" when there are boxes and every one passes <paramref name="test"/>.</summary>
    string On(Func<TextBox, bool> test)
    {
        if (Boxes.Count == 0)
        {
            return "";
        }
        foreach (TextBox t in Boxes)
        {
            if (!test(t))
            {
                return "";
            }
        }
        return "on";
    }

    /// <summary>True when every box passes <paramref name="test"/> (also when there are none).</summary>
    bool Every(Func<TextBox, bool> test)
    {
        foreach (TextBox t in Boxes)
        {
            if (!test(t))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>The standard sizes plus any size the boxes already use, smallest first, each once.</summary>
    List<double> SizeChoices
    {
        get
        {
            List<double> result = new List<double>();
            foreach (double size in TextLook.Sizes)
            {
                if (!result.Contains(size))
                {
                    result.Add(size);
                }
            }
            foreach (TextBox t in Boxes)
            {
                if (!result.Contains(t.FontSize))
                {
                    result.Add(t.FontSize);
                }
            }
            result.Sort();
            return result;
        }
    }

    /// <summary>The shared font size, or "" when the boxes differ.</summary>
    string SizeValue
    {
        get
        {
            if (Boxes.Count == 0 || !Every(t => t.FontSize == Boxes[0].FontSize))
            {
                return "";
            }
            return F(Boxes[0].FontSize);
        }
    }

    bool AllBold
    {
        get
        {
            return Every(t => t.Bold);
        }
    }

    bool AllRounded
    {
        get
        {
            return Every(t => t.Rounded);
        }
    }

    bool AnyBorder
    {
        get
        {
            return !Every(t => t.Border == TextLook.NoBorder);
        }
    }

    /// <summary>Weight the border pattern samples are drawn at: the boxes' own when they agree.</summary>
    string SampleWeight
    {
        get
        {
            if (Boxes.Count == 0 || !Every(t => t.BorderWeight == Boxes[0].BorderWeight))
            {
                return LineLook.Regular;
            }
            return Boxes[0].BorderWeight;
        }
    }
}
