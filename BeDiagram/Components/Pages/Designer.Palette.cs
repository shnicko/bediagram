using Microsoft.AspNetCore.Components.Web;

namespace BeDiagram.Components.Pages;

/// <summary>Drops from the palette (Components/Bars/ShapePalette) onto the canvas: the drop runs the action the tile chose
/// when its drag started (<see cref="IDesignerHost.SetPaletteDrop"/>).</summary>
public partial class Designer
{
    // ───────────── Palette drop ─────────────

    void OnDrop(DragEventArgs e)
    {
        if (paletteDrop is null) return;
        (double x, double y) = ToWorld(e.ClientX, e.ClientY);
        paletteDrop(x, y);
        paletteDrop = null;
    }

    void DragOver() { }
}
