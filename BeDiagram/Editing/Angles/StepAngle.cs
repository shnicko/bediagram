namespace BeDiagram.Editing.Angles;

/// <summary>
/// Locks the offset to the nearest multiple of <see cref="Degrees"/> (90 = horizontal / vertical only).
/// The distance is the pointer's projection onto that direction, so the item follows the pointer along the line.
/// </summary>
public class StepAngle : IAngleLock
{
    const double GridStep = 10;

    public StepAngle(int degrees)
    {
        Degrees = degrees;
    }

    public int Degrees { get; }

    public string Key => Degrees.ToString();
    public string Label => $"{Degrees}°";
    public string Name => Degrees == 90 ? "Lock to horizontal / vertical" : $"Lock to {Degrees}° steps";

    public LockedOffset Apply(double dx, double dy, bool snap)
    {
        if (dx == 0 && dy == 0) return new LockedOffset(0, 0, false, false);
        int steps = 360 / Degrees;
        int k = (int)Math.Round(Math.Atan2(dy, dx) / (2 * Math.PI) * steps);
        k = ((k % steps) + steps) % steps;
        int angle = k * Degrees;

        // Straight along an axis: exact zero on the other axis, and the moving axis snaps to the grid as usual.
        switch (angle)
        {
            case 0: case 180: return new LockedOffset(dx, 0, true, false);
            case 90: case 270: return new LockedOffset(0, dy, false, true);
        }

        double rad = angle * Math.PI / 180;
        double cos = Math.Cos(rad);
        double sin = Math.Sin(rad);
        double length = dx * cos + dy * sin;
        if (snap) length = Math.Round(length / GridStep) * GridStep;
        return new LockedOffset(length * cos, length * sin, false, false);
    }
}
