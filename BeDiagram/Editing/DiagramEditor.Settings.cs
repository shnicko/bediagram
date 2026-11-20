namespace BeDiagram.Editing;

/// <summary>
/// Working settings that feel like the user's, not the diagram's: Auto-join, Snap and the angle lock. With several
/// diagrams open in tabs the page copies them onto the tab it switches to, so they stay the same in every tab.
/// </summary>
public partial class DiagramEditor
{
    /// <summary>Takes Auto-join, Snap and the angle lock (and what its toggle key returns to) from <paramref name="other"/>.</summary>
    public void CopySettingsFrom(DiagramEditor other)
    {
        if (other == this)
        {
            return;
        }
        AutoConnect = other.AutoConnect;
        Snap = other.Snap;
        angleLock = other.angleLock;
        LastAngleLock = other.LastAngleLock;
    }
}
