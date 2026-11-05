namespace BeDiagram.Editing.Angles;

/// <summary>
/// How far a drag may turn (Strategy pattern): free, or locked to steps of 90°, 45° or 30°.
/// Used when moving items and when placing a new joined step, so the line from the start point
/// (or the source shape) to the pointer runs at an allowed angle.
/// </summary>
public interface IAngleLock
{
    /// <summary>Key used by the toolbar ("free", "90", "45", "30").</summary>
    string Key { get; }

    /// <summary>Short button text.</summary>
    string Label { get; }

    /// <summary>Tooltip.</summary>
    string Name { get; }

    /// <summary>
    /// Turns a pointer offset into the nearest allowed one. With <paramref name="snap"/> on, a diagonal offset's
    /// length is rounded to the 10 px grid step (per-axis grid snapping would break the angle).
    /// </summary>
    LockedOffset Apply(double dx, double dy, bool snap);
}
