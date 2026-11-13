using BeDiagram.Models;

namespace BeDiagram.Models.Styles;

/// <summary>The starting looks of the page title and description (<see cref="Diagram.TitleLook"/>,
/// <see cref="Diagram.DescriptionLook"/>): plain text, no background or border, title large and bold, description smaller in slate.</summary>
public static class PageHeaderLooks
{
    public const double TitleFontSize = 20;
    public const double DescriptionFontSize = 12;
    public const string DescriptionColour = "slate";

    public static TextBox NewTitleLook()
    {
        TextBox look = new TextBox();
        look.Font = TextLook.Sans;
        look.FontSize = TitleFontSize;
        look.Bold = true;
        look.Align = TextLook.Left;
        look.VAlign = TextLook.Top;
        return look;
    }

    public static TextBox NewDescriptionLook()
    {
        TextBox look = new TextBox();
        look.Font = TextLook.Sans;
        look.FontSize = DescriptionFontSize;
        look.TextColor = DescriptionColour;
        look.Align = TextLook.Left;
        look.VAlign = TextLook.Top;
        return look;
    }
}
