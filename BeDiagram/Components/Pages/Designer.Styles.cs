using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BeDiagram.Models.Presets;
using BeDiagram.Services;

namespace BeDiagram.Components.Pages;

/// <summary>
/// Copy style, paste style, Paste special and the format painter. The copied style lives in the
/// <see cref="StyleClipboard"/> service, so it survives switching tabs and can be pasted in another window; the
/// pasting itself is the editor's (<c>DiagramEditor.CopyStyle.cs</c>). The painter is a page mode: while it's on, a
/// press on an item paints the look on it instead of selecting or dragging.
/// </summary>
public partial class Designer
{
    // ───────────── Copy style and the format painter ─────────────

    [Inject] StyleClipboard Styles { get; set; } = default!;

    /// <summary>The format painter is on: the next press on an item paints the copied look on it.</summary>
    bool painting;

    /// <summary>The painter stays on after painting (Shift+P / double-click the brush) until Esc.</summary>
    bool paintSticky;

    bool CanCopyStyle
    {
        get
        {
            return Editor.SelectedEdge is not null || Editor.Selection.Count == 1;
        }
    }

    void CopyStyle()
    {
        CopiedStyle? style = Editor.CopyStyleOfSelection();
        if (style is null)
        {
            ShowToast("Select one item or connector to copy its style");
            return;
        }
        Styles.Hold(style);
        ShowToast($"Copied the style of {style.Summary}: select items and press Ctrl+Alt+V to paste it");
    }

    void PasteStyle()
    {
        PasteStyle(PasteParts.LookOnly());
    }

    void PasteStyle(PasteParts parts)
    {
        CopiedStyle? style = Styles.Held;
        if (style is null)
        {
            ShowToast("Copy a style first (Ctrl+Alt+C)");
            return;
        }
        (int applied, int skipped) = Editor.PasteStyle(style, parts);
        ShowToast(PasteMessage(style, applied, skipped));
    }

    static string PasteMessage(CopiedStyle style, int applied, int skipped)
    {
        if (applied == 0)
        {
            return $"The copied style is from a {style.Kind}: select a {style.Kind} to paste it on";
        }
        string onto;
        if (applied == 1)
        {
            onto = $"Pasted onto 1 {style.Kind}";
        }
        else
        {
            onto = $"Pasted onto {applied} {style.KindPlural}";
        }
        if (skipped > 0)
        {
            return $"{onto} ({skipped} other item{(skipped == 1 ? "" : "s")} skipped: not a {style.Kind})";
        }
        return onto;
    }

    void ShowPasteSpecial()
    {
        if (Styles.Held is null)
        {
            ShowToast("Copy a style first (Ctrl+Alt+C)");
            return;
        }
        CloseMenus();
        dialog = Dialog.PasteSpecial;
    }

    /// <summary>Paste special answered: remember the choice for next time and paste.</summary>
    void PasteSpecialChosen(PasteParts parts)
    {
        Styles.LastSpecial = parts;
        CloseDialog();
        PasteStyle(parts);
    }

    /// <summary>
    /// The brush: copies the selected item's look (or keeps the style already copied) and waits for a click. A second
    /// press of the single-shot painter turns it off again.
    /// </summary>
    void StartPainter(bool sticky)
    {
        if (painting && !sticky)
        {
            StopPainter();
            return;
        }
        CopiedStyle? style = Editor.CopyStyleOfSelection();
        if (style is not null)
        {
            Styles.Hold(style);
        }
        else if (Styles.Held is null)
        {
            ShowToast("Select the item whose look you want to copy, then pick the format painter");
            return;
        }
        painting = true;
        paintSticky = sticky;
        CopiedStyle held = Styles.Held!;
        if (sticky)
        {
            ShowToast($"Format painter: click {held.KindPlural} to give them the look of {held.Summary} · Esc to stop");
        }
        else
        {
            ShowToast($"Format painter: click a {held.Kind} to give it the look of {held.Summary}");
        }
    }

    void StopPainter()
    {
        painting = false;
        paintSticky = false;
    }

    /// <summary>
    /// A press on item or connector <paramref name="id"/> while the painter is on: paints the look on it (the
    /// single-shot painter then stops). True when the press was the painter's, so it doesn't also select or drag.
    /// </summary>
    bool PaintAt(PointerEventArgs e, string id)
    {
        if (!painting || e.Button != 0)
        {
            return false;
        }
        CopiedStyle? style = Styles.Held;
        if (style is null)
        {
            StopPainter();
            return false;
        }
        if (Editor.PaintStyle(style, id))
        {
            if (!paintSticky)
            {
                StopPainter();
            }
        }
        else
        {
            ShowToast($"The format painter has the look of a {style.Kind}: click a {style.Kind}");
        }
        return true;
    }

    /// <summary>The brush button's class: lit while the painter is on.</summary>
    string PainterClass
    {
        get
        {
            if (painting)
            {
                return "on";
            }
            return "";
        }
    }
}
