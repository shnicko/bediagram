using BeDiagram.Editing;
using BeDiagram.Editing.Angles;
using BeDiagram.Editing.Guides;
using BeDiagram.Models;
using BeDiagram.Models.Location;

namespace BeDiagram.Components.Pages.Gestures;

/// <summary>
/// Dragging the selection (<see cref="MoveSet"/>). The item under the pointer leads: its snap point
/// (<see cref="IPlacement.SnapPoint"/>: a shape's or group's centre, a text box's top centre) lines up with the centre of
/// another shape when it comes within <see cref="GuideReachPx"/> screen pixels of one (<see cref="CentreGuides"/>, drawn
/// as <see cref="Guides"/>), and otherwise snaps to the grid. Everything else moves by the same amount, so the selection
/// keeps its layout, and a group carries what's inside it. With an angle lock on, the move keeps to an allowed angle and
/// an axis the lock holds still neither snaps nor lines up, so items stay in line.
/// </summary>
public class MoveGesture : UndoableDragGesture
{
    /// <summary>How close, in screen pixels, the leading item's snap point must come to a shape's centre line to line
    /// up with it.</summary>
    public const double GuideReachPx = 8;

    readonly MoveSet moving;
    readonly List<IBox> movers;
    readonly Dictionary<string, (double X, double Y)> origins;
    readonly IBox lead;
    readonly CentreGuides centres;
    List<GuideLine> guides = new List<GuideLine>();

    /// <param name="moving">What the drag moves (<see cref="DiagramEditor.MoveSetFor"/>).</param>
    /// <param name="grabbedId">The item the pointer went down on: it leads the move.</param>
    public MoveGesture(IEditorContext ctx, double x, double y, MoveSet moving, string grabbedId) : base(ctx, x, y)
    {
        this.moving = moving;
        movers = moving.Movers;
        origins = new Dictionary<string, (double X, double Y)>();
        foreach (IBox b in moving.Items)
        {
            origins.Add(b.Id, (b.X, b.Y));
        }
        lead = LeadOf(moving, grabbedId);
        centres = new CentreGuides(ctx.Diagram, new HashSet<string>(origins.Keys));
    }

    /// <summary>The centre guides the leading item is on right now, for the canvas to draw.</summary>
    public List<GuideLine> Guides
    {
        get
        {
            return guides;
        }
    }

    /// <summary>The item that leads: the one grabbed, or the first mover when the grabbed one isn't moving (a shape
    /// while shapes are locked in place).</summary>
    static IBox LeadOf(MoveSet moving, string grabbedId)
    {
        foreach (IBox b in moving.Items)
        {
            if (b.Id == grabbedId)
            {
                return b;
            }
        }
        return moving.Movers[0];
    }

    protected override bool Drag(double dx, double dy)
    {
        if (!Moved && Math.Abs(dx) + Math.Abs(dy) < 2)
        {
            return false;
        }
        Moved = true;
        LockedOffset o = Ctx.LockOffset(dx, dy);
        (double leadX, double leadY) = origins[lead.Id];
        double rawX = leadX + o.Dx;
        double rawY = leadY + o.Dy;
        IPlacement p = lead.Placement;

        // On each axis the angle lock lets snap, a shape's centre line within reach wins over the grid.
        double reach = GuideReachPx / Ctx.Zoom;
        (double snapX, double snapY) = p.SnapPoint(lead, rawX, rawY);
        double? guideX = null;
        double? guideY = null;
        if (o.SnapX)
        {
            guideX = centres.NearestX(snapX, reach);
        }
        if (o.SnapY)
        {
            guideY = centres.NearestY(snapY, reach);
        }
        (double x, double y) = p.SnapTopLeft(lead, rawX, rawY, o.SnapX && guideX is null, o.SnapY && guideY is null, Ctx.SnapV);
        if (guideX is not null)
        {
            x = rawX + (guideX.Value - snapX);
        }
        if (guideY is not null)
        {
            y = rawY + (guideY.Value - snapY);
        }

        // Everything moves by the lead's move, so the selection keeps its layout; a group carries what's inside it.
        double moveX = x - leadX;
        double moveY = y - leadY;
        foreach (IBox b in movers)
        {
            (double ox, double oy) = origins[b.Id];
            double toX = ox + moveX;
            double toY = oy + moveY;
            if (b == lead)
            {
                toX = x;
                toY = y;
            }
            b.Placement.MoveTo(b, toX, toY, Ctx.Tree, moving.Carry);
        }

        (double atX, double atY) = p.SnapPoint(lead, lead.X, lead.Y);
        guides = centres.Lines(atX, atY, guideX, guideY);
        return true;
    }

    protected override void Finished()
    {
        guides = new List<GuideLine>();
        // The roots (items not carried by a group that moved with them) may have entered or left a group. Groups count
        // too: a group dropped wholly inside another group nests in it.
        Ctx.UpdateMembership(moving.Roots);
    }
}
