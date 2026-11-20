using Microsoft.AspNetCore.Components;
using BeDiagram.Editing;
using BeDiagram.Services;

namespace BeDiagram.Components.Pages;

/// <summary>
/// Copy, cut and paste of items (Ctrl+C / Ctrl+X / Ctrl+V). What's copied is kept by the <see cref="ItemClipboard"/>
/// service, so it can be pasted into another tab or window; the copying and pasting are the editor's
/// (<c>DiagramEditor.Clipboard.cs</c>).
/// </summary>
public partial class Designer
{
    // ───────────── Copy and paste items ─────────────

    [Inject] ItemClipboard Items { get; set; } = default!;

    void CopyItems()
    {
        CopiedItems? copied = Editor.CopySelectedItems();
        if (copied is null)
        {
            ShowToast("Select shapes, text boxes or groups to copy");
            return;
        }
        Items.Hold(copied);
        ShowToast($"Copied {ItemCount(copied.Count)}: Ctrl+V pastes them here or in another tab");
    }

    void CutItems()
    {
        Editor.EndEdit();
        CopiedItems? copied = Editor.CutSelectedItems();
        if (copied is null)
        {
            ShowToast("Select shapes, text boxes or groups to cut");
            return;
        }
        Items.Hold(copied);
        ShowToast($"Cut {ItemCount(copied.Count)}: Ctrl+V pastes them");
    }

    void PasteItems()
    {
        CopiedItems? copied = Items.Held;
        if (copied is null)
        {
            ShowToast("Copy something first (Ctrl+C)");
            return;
        }
        Editor.EndEdit();
        double offset = Editor.PasteOffset(copied);
        Editor.PasteItems(copied, offset, offset);
        ShowToast($"Pasted {ItemCount(copied.Count)}");
    }

    static string ItemCount(int count)
    {
        if (count == 1)
        {
            return "1 item";
        }
        return $"{count} items";
    }
}
