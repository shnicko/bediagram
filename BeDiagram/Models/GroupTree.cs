namespace BeDiagram.Models;

/// <summary>
/// The group hierarchy of a diagram (Composite pattern). <see cref="IGroupable"/> is the component: shapes and
/// text boxes are leaves, a <see cref="Group"/> is the composite whose children are the items (including other
/// groups) whose <c>GroupId</c> points at it. The file keeps the flat lists with a parent id on each item, so
/// the tree is worked out here from the diagram each time it's asked for; it never goes out of date.
/// Every walk keeps a visited set, so a damaged file with a parent loop can't hang the editor.
/// </summary>
public class GroupTree
{
    readonly Diagram d;

    public GroupTree(Diagram diagram)
    {
        d = diagram;
    }

    /// <summary>The group directly holding <paramref name="item"/> (null at the top level).</summary>
    public Group? Parent(IGroupable item) =>
        item.GroupId is null ? null : d.Groups.FirstOrDefault(g => g.Id == item.GroupId && !ReferenceEquals(g, item));

    /// <summary>Direct children: shapes, then text boxes, then groups (each in file order).</summary>
    public IEnumerable<IGroupable> Children(Group g) =>
        d.Nodes.Where(n => n.GroupId == g.Id).Cast<IGroupable>()
            .Concat(d.TextBoxes.Where(t => t.GroupId == g.Id))
            .Concat(d.Groups.Where(c => c.GroupId == g.Id && !ReferenceEquals(c, g)));

    /// <summary>Everything inside <paramref name="g"/>, at any depth (parents before their children).</summary>
    public IEnumerable<IGroupable> Descendants(Group g)
    {
        HashSet<string> seen = new HashSet<string> { g.Id };
        Stack<Group> todo = new Stack<Group>();
        todo.Push(g);
        while (todo.Count > 0)
        {
            Group current = todo.Pop();
            foreach (IGroupable child in Children(current))
            {
                if (!seen.Add(child.Id)) continue;
                yield return child;
                if (child is Group cg) todo.Push(cg);
            }
        }
    }

    /// <summary>The groups around <paramref name="item"/>, innermost first.</summary>
    public IEnumerable<Group> Ancestors(IGroupable item)
    {
        HashSet<string> seen = new HashSet<string> { item.Id };
        Group? p = Parent(item);
        while (p is not null && seen.Add(p.Id))
        {
            yield return p;
            p = Parent(p);
        }
    }

    /// <summary>True when <paramref name="item"/> sits inside <paramref name="g"/> at any depth.</summary>
    public bool IsInside(IGroupable item, Group g) => Ancestors(item).Any(a => a.Id == g.Id);

    /// <summary>
    /// Groups in drawing order: each top-level group (file order) followed by the groups inside it, so an inner
    /// group is drawn over its parent. Without nesting this is simply file order. Groups caught in a parent loop
    /// (only possible in a hand-edited file) come last.
    /// </summary>
    public List<Group> PaintOrder()
    {
        List<Group> order = new List<Group>();
        HashSet<string> seen = new HashSet<string>();
        void Visit(Group g)
        {
            if (!seen.Add(g.Id)) return;
            order.Add(g);
            foreach (Group c in d.Groups.Where(c => c.GroupId == g.Id)) Visit(c);
        }
        foreach (Group g in d.Groups.Where(g => Parent(g) is null)) Visit(g);
        foreach (Group g in d.Groups) Visit(g);
        return order;
    }

    /// <summary>The innermost group holding every one of <paramref name="items"/> (null when that's the top level).</summary>
    public Group? CommonParent(IReadOnlyList<IGroupable> items)
    {
        if (items.Count == 0) return null;
        foreach (Group candidate in Ancestors(items[0]))
            if (items.All(i => IsInside(i, candidate))) return candidate;
        return null;
    }
}
