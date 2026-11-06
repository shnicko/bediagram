using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Shared;

/// <summary>
/// A small "i" button that shows or hides a note explaining the control next to it. Keeps the panels short: help text
/// is there on a click instead of always on screen. The note opens in place, under the line the button sits on.
/// </summary>
public partial class InfoTip
{
    /// <summary>The explanation shown when the button is clicked.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    bool Open { get; set; }

    string ButtonClass => Open ? "info-btn on" : "info-btn";

    string ButtonTitle => Open ? "Hide" : "What's this?";

    void Toggle() => Open = !Open;
}
