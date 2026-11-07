using System.Globalization;
using BeDiagram.Editing;
using BeDiagram.Models;
using BeDiagram.Models.Styles;
using Microsoft.AspNetCore.Components;

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

    Task Set(Action<TextBox> change) { Editor.Restyle(Boxes, change); return OnChanged.InvokeAsync(); }

    Task SetFontSize(object? value) { Editor.SetFontSize(Boxes, value); return OnChanged.InvokeAsync(); }

    static readonly (string Key, string Name)[] HAligns = { (TextLook.Left, "Left"), (TextLook.Center, "Centre"), (TextLook.Right, "Right") };
    static readonly (string Key, string Name)[] VAligns = { (TextLook.Top, "Top"), (TextLook.Middle, "Middle"), (TextLook.Bottom, "Bottom") };

    static string F(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);

    string On(Func<TextBox, bool> test) => Boxes.Count > 0 && Boxes.All(test) ? "on" : "";

    /// <summary>The standard sizes plus any size the boxes already use.</summary>
    IEnumerable<double> SizeChoices => TextLook.Sizes.Concat(Boxes.Select(t => t.FontSize)).Distinct().OrderBy(v => v);

    /// <summary>The shared font size, or "" when the boxes differ.</summary>
    string SizeValue => Boxes.Select(t => t.FontSize).Distinct().Count() == 1 ? F(Boxes[0].FontSize) : "";

    bool AllBold => Boxes.All(t => t.Bold);

    bool AllRounded => Boxes.All(t => t.Rounded);

    bool AnyBorder => Boxes.Any(t => t.Border != TextLook.NoBorder);

    /// <summary>Weight the border pattern samples are drawn at: the boxes' own when they agree.</summary>
    string SampleWeight => Boxes.Select(t => t.BorderWeight).Distinct().Count() == 1 ? Boxes[0].BorderWeight : LineLook.Regular;
}
