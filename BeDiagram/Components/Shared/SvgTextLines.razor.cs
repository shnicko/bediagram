using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Shared;

/// <summary>Lines of SVG text in a canvas layer (a shape's label or description): one <c>text</c> with a
/// <c>tspan</c> per line, each with its line offset. A null <see cref="Fill"/> or <see cref="FontWeight"/> leaves the
/// attribute out, so the CSS class decides.</summary>
public partial class SvgTextLines
{
    [Parameter, EditorRequired] public string Class { get; set; } = "";
    [Parameter, EditorRequired] public double X { get; set; }
    [Parameter, EditorRequired] public double Y { get; set; }
    [Parameter] public string? Fill { get; set; }
    [Parameter] public string? FontWeight { get; set; }
    /// <summary>Each line's text and its <c>dy</c> (0 for the first line, then one line height).</summary>
    [Parameter, EditorRequired] public List<(string Text, string Dy)> Lines { get; set; } = new List<(string Text, string Dy)>();
}
