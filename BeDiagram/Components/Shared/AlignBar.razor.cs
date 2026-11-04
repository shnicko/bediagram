using Microsoft.AspNetCore.Components;

namespace BeDiagram.Components.Shared;

/// <summary>Row of align / distribute buttons (used for a multi-selection and inside a group).</summary>
public partial class AlignBar
{
    static readonly AlignButton[] Buttons =
    {
        new(AlignOp.Left, "align-start-vertical", "Align left"),
        new(AlignOp.CenterX, "align-center-vertical", "Align centres (vertical line)"),
        new(AlignOp.Right, "align-end-vertical", "Align right"),
        new(AlignOp.Top, "align-start-horizontal", "Align top"),
        new(AlignOp.Middle, "align-center-horizontal", "Align middles (horizontal line)"),
        new(AlignOp.Bottom, "align-end-horizontal", "Align bottom"),
        new(AlignOp.DistributeH, "align-horizontal-distribute-center", "Distribute horizontally"),
        new(AlignOp.DistributeV, "align-vertical-distribute-center", "Distribute vertically"),
    };

    [Parameter] public bool Enabled { get; set; } = true;
    [Parameter] public EventCallback<AlignOp> OnAlign { get; set; }

    Task Click(AlignOp op) => OnAlign.InvokeAsync(op);
}
