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
    static readonly List<PaletteShape> PaletteShapes = BuildPaletteShapes();

    /// <summary>The classic shapes, each offered again as a circle with its type's icon (<see cref="Node.Round"/>).</summary>
    static readonly List<NodeTypeInfo> RoundShapes = BuildRoundShapes();

    static List<PaletteShape> BuildPaletteShapes()
    {
        List<PaletteShape> result = new List<PaletteShape>();
        foreach (NodeTypeInfo t in NodeTypes.All)
        {
            if (!t.Kind.HasOutcome)
            {
                result.Add(new PaletteShape(t, result.Count + 1));
            }
        }
        return result;
    }

    static List<NodeTypeInfo> BuildRoundShapes()
    {
        List<NodeTypeInfo> result = new List<NodeTypeInfo>();
        foreach (NodeTypeInfo t in NodeTypes.All)
        {
            if (t.Kind.CanBeRound)
            {
                result.Add(t);
            }
        }
        return result;
    }

    static PaletteItem OutcomeItem(OutcomeInfo o)
    {
        return new PaletteItem(NodeType.Outcome, o.Key);
    }

    static PaletteItem RoundItem(NodeTypeInfo t)
    {
        return new PaletteItem(t.Key, Round: true);
    }

    static string RoundPaletteTip(NodeTypeInfo t)
    {
        return $"{t.Display} as a circle: drag onto the canvas or click to add";
    }

    static string OutcomePaletteTip(OutcomeInfo o)
    {
        return $"{o.Display}: {o.Hint}. Drag onto the canvas or click to add.";
    }

    /// <summary>Tooltip for a shape tile in the palette (the tiles have no text of their own).</summary>
    static string PaletteShapeTip(PaletteShape p)
    {
        if (p.HasKey)
        {
            return $"{p.Type.Display}: drag onto the canvas, click to add, or press {p.Number}";
        }
        return $"{p.Type.Display}: drag onto the canvas or click to add";
    }

    /// <summary>Extra lines inside a palette tile (the title band and rows of a list / IPO box), or null.</summary>
    static string? PaletteInnerPath(NodeType type)
    {
        return NodeTypes.Get(type).Kind.PaletteInnerPath;
    }

    static string PaletteShapePath(NodeType type)
    {
        return SvgShapes.ShapePath(new Node { Type = type, W = 48, H = 28 });
    }

    void StartTextDrag()
    {
        Host.SetPaletteDrop((x, y) => Editor.AddTextAt(x - 100, y - 30, null, comment: false));
    }

    /// <summary>A comment dropped on a shape is pinned to it.</summary>
    void StartCommentDrag()
    {
        Host.SetPaletteDrop((x, y) => Editor.AddTextAt(x - 100, y - 55, PinTarget(x, y), comment: true));
    }

    /// <summary>The id of the shape at (<paramref name="x"/>, <paramref name="y"/>), or null.</summary>
    string? PinTarget(double x, double y)
    {
        if (Editor.HitTest(x, y) is Node n)
        {
            return n.Id;
        }
        return null;
    }

    void StartGroupDrag()
    {
        Host.SetPaletteDrop((x, y) => Editor.AddGroupAt(x, y));
    }

    void StartPaletteDrag(PaletteItem item)
    {
        Host.SetPaletteDrop((x, y) => Editor.AddNodeAt(item, x, y, join: Editor.AutoConnect, lockAngle: true));
    }
}
