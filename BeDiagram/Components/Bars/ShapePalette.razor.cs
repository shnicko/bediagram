using BeDiagram.Components.Pages;
using BeDiagram.Components.Pages.Views;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Rendering;

namespace BeDiagram.Components.Bars;

/// <summary>The palette down the left: shape, circle, outcome, text, comment and group tiles. Click a tile to add it next to the selection, or drag it onto the canvas (the page adds it where it is dropped).</summary>
public partial class ShapePalette : DesignerPart
{
    /// <summary>Palette shapes (outcomes have their own section), numbered for the 1–9 shortcut keys.</summary>
    static readonly List<PaletteShape> PaletteShapes = NodeTypes.All
        .Where(t => !t.Kind.HasOutcome)
        .Select((t, i) => new PaletteShape(t, i + 1))
        .ToList();

    /// <summary>The classic shapes, each offered again as a circle with its type's icon (<see cref="Node.Round"/>).</summary>
    static readonly List<NodeTypeInfo> RoundShapes = NodeTypes.All.Where(t => t.Kind.CanBeRound).ToList();

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

    void StartTextDrag() => Host.SetPaletteDrop((x, y) => Editor.AddTextAt(x - 100, y - 30, null, comment: false));

    void StartCommentDrag() => Host.SetPaletteDrop((x, y) => Editor.AddTextAt(x - 100, y - 55, (Editor.HitTest(x, y) as Node)?.Id, comment: true));

    void StartGroupDrag() => Host.SetPaletteDrop((x, y) => Editor.AddGroupAt(x, y));

    void StartPaletteDrag(PaletteItem item) =>
        Host.SetPaletteDrop((x, y) => Editor.AddNodeAt(item, x, y, join: Editor.AutoConnect, lockAngle: true));
}
