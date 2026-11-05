namespace BeDiagram.Editing.Angles;

/// <summary>No lock: the offset is used as it is and both axes snap to the grid as usual.</summary>
public class FreeAngle : IAngleLock
{
    public string Key => "free";
    public string Label => "Free";
    public string Name => "Move and place at any angle";

    public LockedOffset Apply(double dx, double dy, bool snap) => new LockedOffset(dx, dy, true, true);
}
