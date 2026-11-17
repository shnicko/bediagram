using BeDiagram.Models;

namespace BeDiagram.Editing;

/// <summary>
/// What a drag or nudge of the selection moves, worked out in one place by
/// <see cref="DiagramEditor.MoveSetFor"/>: every item that ends up moving, the ones the move starts from, and whether
/// groups carry their contents.
/// </summary>
public class MoveSet
{
    public MoveSet(List<IBox> items, List<IGroupable> roots, bool carry)
    {
        Items = items;
        Roots = roots;
        Carry = carry;
    }

    /// <summary>Every item that ends up moving: with <see cref="Carry"/> the selection plus everything inside the
    /// selected groups, without it just the selected items (only group boxes while shapes are locked).</summary>
    public List<IBox> Items { get; }

    /// <summary>The items whose own group isn't moving with them. They are what a move starts from, the groups around
    /// them may need to grow, and dropping them can change which group they are in.</summary>
    public List<IGroupable> Roots { get; }

    /// <summary>True when a group carries everything inside it; false with shapes locked in place or Alt held on a
    /// group's title (just the boxes move).</summary>
    public bool Carry { get; }

    /// <summary>The items to move one by one: the roots when groups carry their contents (each moves what's inside
    /// it), otherwise every item.</summary>
    public List<IBox> Movers
    {
        get
        {
            if (!Carry)
            {
                return Items;
            }
            List<IBox> result = new List<IBox>();
            foreach (IGroupable root in Roots)
            {
                result.Add(root);
            }
            return result;
        }
    }
}
