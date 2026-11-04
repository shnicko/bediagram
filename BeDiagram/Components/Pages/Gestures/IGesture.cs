namespace BeDiagram.Components.Pages.Gestures;

/// <summary>A pointer position: in screen px (for panning) and in world coordinates (for everything else).</summary>
public record GesturePoint(double ClientX, double ClientY, double X, double Y);

/// <summary>
/// State pattern: one mouse gesture in progress (move, resize, connect, pan, marquee, connector end, connector bend).
/// The Designer holds the current gesture (null when the pointer is idle) and hands it every pointer move and the
/// pointer-up. Each gesture class keeps its own data, so adding a gesture never touches the others.
/// </summary>
public interface IGesture
{
    /// <summary>The pointer moved. Returns false when nothing visible changed, so the canvas can skip a render.</summary>
    bool Move(GesturePoint p);

    /// <summary>The pointer was released (or left the canvas): finish the gesture and record undo if needed.</summary>
    void Up();

    /// <summary>Escape or inline editing stopped the gesture before pointer-up. Changes made so far are kept.</summary>
    void Cancel();
}
