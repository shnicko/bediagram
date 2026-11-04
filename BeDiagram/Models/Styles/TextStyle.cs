namespace BeDiagram.Models.Styles;

/// <summary>Resolved colours and lines for drawing a text box (canvas and export).</summary>
public record TextStyle(string? Fill, string? Stroke, double StrokeWidth, string? Dash, string Cap, double Radius,
                        string Family, string TextColour, string FoldColour)
{
    public const string DefaultBorder = "#9aa3ad";
    public const string DefaultText = "#1f2933";

    public static TextStyle For(TextBox t)
    {
        bool hasBorder = t.Border != TextLook.NoBorder;
        double w = LineLook.Width(t.BorderWeight);
        string stroke = TextPalette.StrokeOf(t.BorderColor, DefaultBorder);
        // Folded corner follows the border colour, or the background's own edge colour when there is no border.
        string fold = hasBorder ? stroke : TextPalette.StrokeOf(t.Fill, DefaultBorder);
        return new TextStyle(
            TextPalette.FillOf(t.Fill),
            hasBorder ? stroke : null,
            w,
            hasBorder ? LineLook.Dash(t.Border, w) : null,
            LineLook.Cap(t.Border),
            t.Rounded ? 4 : 0,
            TextLook.Family(t.Font),
            TextPalette.StrokeOf(t.TextColor, DefaultText),
            fold);
    }
}
