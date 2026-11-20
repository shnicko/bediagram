using Microsoft.AspNetCore.Components;
using BeDiagram.Models.Presets;

namespace BeDiagram.Components.Dialogs;

/// <summary>
/// Paste special: the copied look plus the parts ticked here. Only the parts the copied kind of item has are offered.
/// Starts with the last choice (<see cref="Start"/>).
/// </summary>
public partial class PasteSpecialDialog
{
    [Parameter, EditorRequired] public CopiedStyle Style { get; set; } = default!;
    /// <summary>The choice to start with: the last one made.</summary>
    [Parameter] public PasteParts Start { get; set; } = new PasteParts();
    [Parameter] public EventCallback<PasteParts> OnPaste { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    PasteParts Parts = new PasteParts();

    protected override void OnInitialized()
    {
        Parts = Start.Clone();
    }

    string TextLabel
    {
        get
        {
            if (Style.Shape is not null)
            {
                return "Text (label and description)";
            }
            if (Style.Group is not null)
            {
                return "Title";
            }
            if (Style.Connector is not null)
            {
                return "Label";
            }
            return "Text";
        }
    }

    /// <summary>Only the parts on offer go through, so a tick left from another kind of item does nothing.</summary>
    Task Paste()
    {
        PasteParts chosen = Parts.Clone();
        chosen.Size = chosen.Size && PasteParts.SizeFor(Style);
        chosen.Type = chosen.Type && PasteParts.TypeFor(Style);
        chosen.Properties = chosen.Properties && PasteParts.PropertiesFor(Style);
        chosen.Link = chosen.Link && PasteParts.LinkFor(Style);
        return OnPaste.InvokeAsync(chosen);
    }
}
