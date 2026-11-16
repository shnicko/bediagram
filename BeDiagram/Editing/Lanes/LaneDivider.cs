using BeDiagram.Models;

namespace BeDiagram.Editing.Lanes;

/// <summary>
/// The edge two touching lanes share (see <see cref="LaneDividers"/>). <paramref name="Rows"/>: the lanes sit one above
/// the other, so the edge is a horizontal line at y = <paramref name="At"/>; otherwise they sit side by side and the edge
/// is a vertical line at x = <paramref name="At"/>. <paramref name="Spans"/> are the parts of the edge that can be
/// dragged, as (from, to) along it: the stretch both lanes share, short of the corners and with gaps around the ● ports.
/// </summary>
public record LaneDivider(Group Before, Group After, bool Rows, double At, List<(double From, double To)> Spans);
