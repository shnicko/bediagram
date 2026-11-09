using BeDiagram.Components.Pages.Views;
using BeDiagram.Models;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Text;
using BeDiagram.Rendering;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Components.Canvas;

/// <summary>Text boxes and comments, under connectors and shapes: outline or guide, comment fold, text (inline editor while editing), ports and resize handle.</summary>
public partial class TextBoxLayer : CanvasLayer
{

    IEnumerable<TextBoxView> TextBoxViews() =>
        D.TextBoxes.Select(t =>
        {
            TextStyle ts = TextStyle.For(t);
            return new TextBoxView(t, ts, Editor.Selection.Contains(t.Id), TextLayout.Layout(t), SvgShapes.TextBoxPath(t, ts.Radius));
        });

    /// <summary>Inline editor styled like the box itself, so editing looks the same as the finished text.</summary>
    static string TextEditStyle(TextBox t, TextStyle s) =>
        $"font-family:{s.Family};font-size:{F(t.FontSize)}px;line-height:{F(t.FontSize * TextLook.LineSpacing)}px;" +
        $"font-weight:{(t.Bold ? "600" : "400")};color:{s.TextColour};text-align:{t.Align};padding:{F(TextLook.PadY)}px {F(TextLook.PadX)}px;";
}
