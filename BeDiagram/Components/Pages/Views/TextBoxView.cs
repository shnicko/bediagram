using BeDiagram.Models;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;

namespace BeDiagram.Components.Pages.Views;

public record TextBoxView(TextBox T, TextStyle Style, bool Selected, TextBoxLayout Layout, string Outline);
