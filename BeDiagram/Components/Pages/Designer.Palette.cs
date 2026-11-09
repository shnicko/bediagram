using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Rendering;

namespace BeDiagram.Components.Pages;

/// <summary>Palette tiles: dragging a tile onto the canvas (the drop runs the action chosen at drag start).</summary>
public partial class Designer
{
    // ───────────── Palette drag and drop ─────────────

    void OnDrop(DragEventArgs e)
    {
        if (paletteDrop is null) return;
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        paletteDrop(x, y);
        paletteDrop = null;
    }

    static PaletteItem OutcomeItem(OutcomeInfo o) => new(NodeType.Outcome, o.Key);

    static PaletteItem RoundItem(NodeTypeInfo t) => new(t.Key, Round: true);

    static string RoundPaletteTip(NodeTypeInfo t) => $"{t.Display} as a circle: drag onto the canvas or click to add";

    static string OutcomePaletteTip(OutcomeInfo o) => $"{o.Display}: {o.Hint}. Drag onto the canvas or click to add.";

    /// <summary>Tooltip for a shape tile in the palette (the tiles have no text of their own).</summary>
    static string PaletteShapeTip(PaletteShape p) => p.HasKey
        ? $"{p.Type.Display}: drag onto the canvas, click to add, or press {p.Number}"
        : $"{p.Type.Display}: drag onto the canvas or click to add";

    /// <summary>Extra lines inside a palette tile (the title band and rows of a list / IPO box), or null.</summary>
    static string? PaletteInnerPath(NodeType type) => NodeTypes.Get(type).Kind.PaletteInnerPath;

    static string PaletteShapePath(NodeType type) => SvgShapes.ShapePath(new Node { Type = type, W = 48, H = 28 });

    void StartTextDrag() => paletteDrop = (x, y) => Editor.AddTextAt(x - 100, y - 30, null, comment: false);

    void StartCommentDrag() => paletteDrop = (x, y) => Editor.AddTextAt(x - 100, y - 55, (Editor.HitTest(x, y) as Node)?.Id, comment: true);

    void StartGroupDrag() => paletteDrop = (x, y) => Editor.AddGroupAt(x, y);

    void StartPaletteDrag(PaletteItem item) =>
        paletteDrop = (x, y) => Editor.AddNodeAt(item, x, y, join: Editor.AutoConnect, lockAngle: true);

    void DragOver() { }
}
