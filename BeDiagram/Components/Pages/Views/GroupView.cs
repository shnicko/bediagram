using BeDiagram.Models;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;

namespace BeDiagram.Components.Pages.Views;

/// <param name="BandPath">Tinted title band, or null for a plain-text title.</param>
/// <param name="TitleArea">Where the title sits (drag handle and inline editor).</param>
public record GroupView(Group G, Swatch Swatch, bool Selected, string? BandPath, Rect TitleArea,
                        double TitleX, double TitleY, string TitleAnchor, double FillOpacity, string? Dash);
