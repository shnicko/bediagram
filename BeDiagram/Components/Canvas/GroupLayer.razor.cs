using BeDiagram.Components.Pages.Views;
using BeDiagram.Models;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Text;

namespace BeDiagram.Components.Canvas;

/// <summary>Groups, the bottom layer, in paint order (a group inside another is drawn over it): box, title band and title (inline editor while renaming), ports and resize handle.</summary>
public partial class GroupLayer : CanvasLayer
{

    /// <summary>Groups in drawing order: a group inside another is drawn over it.</summary>
    IEnumerable<GroupView> GroupViews() =>
        Editor.Tree.PaintOrder().Select(g =>
        {
            (double ax, double ay, double aw, double ah) = g.TitleArea();
            (double tx, double ty, string anchor) = g.TitlePos();
            double width = GroupLook.Width(g.BorderWeight) + (Editor.Selection.Contains(g.Id) ? 1 : 0);
            return new GroupView(g, Palette.Get(g.Color), Editor.Selection.Contains(g.Id), g.BandPath(), new Rect(ax, ay, aw, ah),
                                 tx, ty, anchor, g.FillOpacity(), LineLook.Dash(g.Border, width), width);
        });

    /// <summary>Selection outline drawn just outside a group (and around a title shown under it).</summary>
    static Rect GroupSelectionRect(Group g) =>
        new Rect(g.X - 5, g.Y - 5, g.W + 10, g.H + 10 + TextLayout.ExtraBelow(g));

    /// <summary>Inline title editor: inside the title area, at least 120px wide.</summary>
    static Rect GroupTitleEditRect(GroupView v) =>
        new Rect(v.TitleArea.X + 8, v.TitleArea.Y + (v.TitleArea.H - 24) / 2, Math.Max(120, v.TitleArea.W - 16), 24);
}
