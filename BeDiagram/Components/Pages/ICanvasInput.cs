using Microsoft.AspNetCore.Components.Web;

namespace BeDiagram.Components.Pages;

/// <summary>
/// Pointer presses on items in the canvas layers (Components/Canvas), handed to the page: each picks the selection and
/// starts the right gesture (State pattern, see Gestures/IGesture.cs). Pointer moves and releases go straight to the
/// page's SVG, so a drag never passes through a layer. Reached as <see cref="IDesignerHost.Canvas"/>; the Designer
/// implements it in Designer.Host.cs.
/// </summary>
public interface ICanvasInput
{
    /// <summary>Pressed on a shape, text box or group title: select it (Shift / Ctrl add) and start a move.</summary>
    void ItemDown(PointerEventArgs e, string id);

    /// <summary>Pressed on a group's body: a click selects the group, a drag draws a selection box inside it
    /// (with Shift / Ctrl, as <see cref="ItemDown"/>).</summary>
    void GroupBodyDown(PointerEventArgs e, string id);

    /// <summary>Pressed on a ● port of an item, or of one row of a list / IPO box (<paramref name="itemId"/>): start
    /// drawing a connector.</summary>
    void PortDown(PointerEventArgs e, string id, string? itemId = null);

    /// <summary>Pressed on the resize handle of the one selected item.</summary>
    void ResizeDown(PointerEventArgs e, string id);

    /// <summary>Pressed on a connector or its label: select it.</summary>
    void EdgeDown(PointerEventArgs e, string id);
}
