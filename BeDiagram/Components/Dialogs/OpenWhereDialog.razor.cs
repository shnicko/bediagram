using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Dialogs;

/// <summary>
/// Asked when opening a diagram while another is on screen: open it in a new tab, or close the one on screen and
/// open it there (that asks about unsaved changes next, as following a link does).
/// </summary>
public partial class OpenWhereDialog
{
    /// <summary>The diagram on screen now.</summary>
    [Parameter] public string OpenName { get; set; } = "";
    /// <summary>The diagram being opened.</summary>
    [Parameter] public string Target { get; set; } = "";
    /// <summary>The diagram on screen has unsaved changes.</summary>
    [Parameter] public bool Dirty { get; set; }
    /// <summary>The answer: true = a new tab, false = in place of the diagram on screen.</summary>
    [Parameter] public EventCallback<bool> OnChoose { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    Task NewTab()
    {
        return OnChoose.InvokeAsync(true);
    }

    Task Replace()
    {
        return OnChoose.InvokeAsync(false);
    }
}
