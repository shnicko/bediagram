using System.Text;
using Microsoft.AspNetCore.Components;
using BeDiagram.Editing;
using BeDiagram.Services.Export;

namespace BeDiagram.Components.Dialogs;

/// <summary>
/// The JSON editor: the whole diagram as file JSON in a text box, to copy out, edit or replace by pasting. While it is
/// open the canvas can't be used (the page ignores shortcuts while any dialog is up), so the text and the diagram can't
/// drift apart. Apply hands the text to <see cref="DiagramEditor.ApplyJson"/>; if that reports problems they are
/// listed and the dialog stays open, otherwise the page closes it. Cancel closes it with the diagram untouched.
/// Clicking outside doesn't close it, so pasted text isn't lost by a stray click.
/// </summary>
public partial class JsonDialog
{
    /// <summary>Most problems listed at once; the rest are counted.</summary>
    const int MaxShown = 20;

    [Inject] IClipboard Clipboard { get; set; } = default!;

    [Parameter, EditorRequired] public DiagramEditor Editor { get; set; } = default!;
    /// <summary>Raised when the JSON was applied; the page closes the dialog and redraws.</summary>
    [Parameter] public EventCallback OnApplied { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>The text in the box. Bound on change (when the box loses focus), so a large paste is sent once, not per key.</summary>
    string text = "";
    List<string> errors = new List<string>();
    /// <summary>"Copied" or why copying failed, shown next to the button; null before the first copy.</summary>
    string? copyNote;

    protected override void OnInitialized()
    {
        text = Editor.ToJson();
    }

    async Task Apply()
    {
        copyNote = null;
        errors = Editor.ApplyJson(text);
        if (errors.Count == 0)
        {
            await OnApplied.InvokeAsync();
        }
    }

    async Task CopyAll()
    {
        try
        {
            await Clipboard.CopyAsync(Encoding.UTF8.GetBytes(text), "text/plain");
            copyNote = "Copied";
        }
        catch (Exception)
        {
            // The browser only allows it on https or localhost, with the page focused and permission given.
            copyNote = "Could not copy here: click in the box, press Ctrl+A then Ctrl+C";
        }
    }

    string ErrorHeading
    {
        get
        {
            if (errors.Count == 1)
            {
                return "1 problem";
            }
            return errors.Count + " problems";
        }
    }

    List<string> ShownErrors
    {
        get
        {
            List<string> shown = new List<string>();
            for (int i = 0; i < errors.Count && i < MaxShown; i++)
            {
                shown.Add(errors[i]);
            }
            return shown;
        }
    }

    int HiddenErrors
    {
        get
        {
            if (errors.Count <= MaxShown)
            {
                return 0;
            }
            return errors.Count - MaxShown;
        }
    }
}
