using BeDiagram.Components.Pages.Gestures;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Models;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Styles;
using BeDiagram.Models.Text;

namespace BeDiagram.Components.Canvas;

/// <summary>Groups, the bottom layer, in paint order (a group inside another is drawn over it): box, title band and title (inline editor while renaming), ports and resize grips (all four corners, and each side).</summary>
public partial class GroupLayer : CanvasLayer
{

    /// <summary>Groups in drawing order: a group inside another is drawn over it.</summary>
    List<GroupView> GroupViews()
    {
        List<GroupView> result = new List<GroupView>();
        foreach (Group g in Editor.Tree.PaintOrder())
        {
            (double ax, double ay, double aw, double ah) = g.TitleArea();
            (double tx, double ty, string anchor) = g.TitlePos();
            bool selected = Editor.Selection.Contains(g.Id);
            double width = GroupLook.Width(g.BorderWeight) + (selected ? 1 : 0);
            result.Add(new GroupView(g, Palette.Get(g.Color), selected, g.BandPath(), new Rect(ax, ay, aw, ah),
                                     tx, ty, anchor, g.FillOpacity(), LineLook.Dash(g.Border, width), width));
        }
        return result;
    }

    /// <summary>Selection outline drawn just outside a group (and around a title shown under it).</summary>
    static Rect GroupSelectionRect(Group g)
    {
        return new Rect(g.X - 5, g.Y - 5, g.W + 10, g.H + 10 + TextLayout.ExtraBelow(g));
    }

    /// <summary>Inline title editor: inside the title area, at least 120px wide.</summary>
    static Rect GroupTitleEditRect(GroupView v)
    {
        return new Rect(v.TitleArea.X + 8, v.TitleArea.Y + (v.TitleArea.H - 24) / 2, Math.Max(120, v.TitleArea.W - 16), 24);
    }

    /// <summary>Resize grips show on a group when it is the only thing selected.</summary>
    bool ShowsGrips(GroupView v)
    {
        return v.Selected && Editor.Selection.Count == 1;
    }

    /// <summary>The visible 12 px grip on a corner of the group.</summary>
    static Rect GripRect(Group g, ResizeHandle h)
    {
        (double x, double y) = h.On(g);
        return new Rect(x - 6, y - 6, 12, 12);
    }

    /// <summary>The invisible 8 px strip along one side of the group, stopping short of the corner grips.</summary>
    static Rect EdgeStripRect(Group g, ResizeHandle h)
    {
        (double x, double y) = h.On(g);
        if (h.V != 0)
        {
            return new Rect(g.X + 8, y - 4, Math.Max(0, g.W - 16), 8);
        }
        return new Rect(x - 4, g.Y + 8, 8, Math.Max(0, g.H - 16));
    }
}
