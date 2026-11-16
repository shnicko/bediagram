using BeDiagram.Models;

namespace BeDiagram.Editing.Lanes;

/// <summary>Where <see cref="LaneStacker"/> puts a lane: its new top-left corner (its size doesn't change).</summary>
public record LanePlace(Group Lane, double X, double Y);
