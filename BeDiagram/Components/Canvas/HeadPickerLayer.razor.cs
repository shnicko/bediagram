using BeDiagram.Components.Pages.Views;
using BeDiagram.Models;
using BeDiagram.Models.Heads;
using BeDiagram.Models.Serialization;
using static BeDiagram.Models.SvgNumbers;

namespace BeDiagram.Components.Canvas;

/// <summary>The head picker: a small pop-up next to a connector's quick-change spot (top layer) listing every head
/// by family. A click sets that end's head (one undo step) and closes it. It stays the same size on screen at any zoom.</summary>
public partial class HeadPickerLayer : CanvasLayer
{
    const double Width = 272;
    const double Height = 186;

    /// <summary>The connector the picker is open on, or null when it's closed (or the connector has gone).</summary>
    Edge? Picked
    {
        get
        {
            HeadPick? pick = Host.HeadPick;
            if (pick is null)
            {
                return null;
            }
            foreach (Edge e in D.Edges)
            {
                if (e.Id == pick.EdgeId)
                {
                    return e;
                }
            }
            return null;
        }
    }

    bool AtStart
    {
        get
        {
            HeadPick? pick = Host.HeadPick;
            return pick is not null && pick.AtStart;
        }
    }

    string Title
    {
        get
        {
            if (AtStart)
            {
                return "Start of the connector";
            }
            return "End of the connector";
        }
    }

    /// <summary>At the spot that was pressed, scaled so the pop-up keeps its size on screen.</summary>
    string PickerTransform
    {
        get
        {
            HeadPick? pick = Host.HeadPick;
            if (pick is null)
            {
                return "";
            }
            return $"translate({F(pick.X)},{F(pick.Y)}) scale({F(1 / Host.Zoom)})";
        }
    }

    string HeadClass(IArrowHead h)
    {
        Edge? e = Picked;
        if (e is null)
        {
            return "head-btn";
        }
        if ((AtStart ? e.StartHead() : e.EndHead()) == h.Key)
        {
            return "head-btn on";
        }
        return "head-btn";
    }

    static string HeadTip(IArrowHead h)
    {
        return $"{h.Name}: {h.Hint}";
    }

    /// <summary>The head's saved name, which the browser tests use to find its button.</summary>
    static string HeadName(IArrowHead h)
    {
        return EnumNames.Of(h.Key);
    }

    void Choose(IArrowHead h)
    {
        Edge? e = Picked;
        if (e is not null)
        {
            Editor.SetEdgeHead(e, AtStart, h.Key);
        }
        Host.CloseHeadPicker();
    }

    void Close()
    {
        Host.CloseHeadPicker();
    }
}
