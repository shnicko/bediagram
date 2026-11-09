namespace BeDiagram.Editing.Angles;

/// <summary>The angle locks offered in the toolbar, in order.</summary>
public static class AngleLocks
{
    public static readonly IAngleLock Free = new FreeAngle();
    public static readonly IAngleLock Right = new StepAngle(90);
    public static readonly IAngleLock Half = new StepAngle(45);
    public static readonly IAngleLock Third = new StepAngle(30);

    public static readonly IReadOnlyList<IAngleLock> All = new[] { Free, Right, Half, Third };
}
