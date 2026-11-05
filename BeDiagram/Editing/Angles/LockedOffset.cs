namespace BeDiagram.Editing.Angles;

/// <summary>
/// An offset after the angle lock. <see cref="SnapX"/> / <see cref="SnapY"/> say whether that axis may still be
/// snapped to the grid: an axis the lock holds still (or a diagonal, where snapping would bend the angle) is not.
/// </summary>
public record LockedOffset(double Dx, double Dy, bool SnapX, bool SnapY);
