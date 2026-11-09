using BeDiagram.Models;
using BeDiagram.Models.Text;

namespace BeDiagram.Editing;

/// <summary>Align, distribute, row / column, centre on items, swap places.</summary>
public partial class DiagramEditor
{
    // ───────────── Alignment ─────────────

    /// <summary>
    /// Aligns/arranges items. Inside a group the group's interior is the reference; otherwise the selection bounds.
    /// Left/centre/right/top/middle/bottom line up the shape outlines only — a label drawn under a circle or outcome
    /// is ignored, so a circle and a box aligned to the bottom share the same bottom edge.
    /// Distribute and column still leave room for those labels so they don't run into the next item.
    /// </summary>
    public void Align(AlignOp op, List<IBox> items, Group? within)
    {
        if (items.Count == 0 || (within is null && items.Count < 2)) return;
        Change();
        double L, T, R, B;
        if (within is not null)
        {
            L = within.X + Group.Pad; R = within.X + within.W - Group.Pad;
            T = within.Y + within.InsetTop() + Group.Pad;
            // Leave room inside the group for a bottom title band and for the tallest label hanging below an outline.
            B = within.Y + within.H - Group.Pad - within.InsetBottom() - items.Max(TextLayout.ExtraBelow);
        }
        else
        {
            L = items.Min(b => b.X); R = items.Max(b => b.X + b.W);
            T = items.Min(b => b.Y); B = items.Max(b => b.Y + b.H);
        }
        const double gap = 40;

        switch (op)
        {
            case AlignOp.Left: foreach (IBox b in items) MoveBox(b, L, b.Y); break;
            case AlignOp.CenterX: foreach (IBox b in items) MoveBox(b, (L + R) / 2 - b.W / 2, b.Y); break;
            case AlignOp.Right: foreach (IBox b in items) MoveBox(b, R - b.W, b.Y); break;
            case AlignOp.Top: foreach (IBox b in items) MoveBox(b, b.X, T); break;
            case AlignOp.Middle: foreach (IBox b in items) MoveBox(b, b.X, (T + B) / 2 - b.H / 2); break;
            case AlignOp.Bottom: foreach (IBox b in items) MoveBox(b, b.X, B - b.H); break;
            case AlignOp.DistributeH:
            {
                // Items sharing a centre line (one above the other) move as one block, so they stay lined up.
                List<List<IBox>> blocks = SameCentre(items, b => b.X + b.W / 2);
                double space = (R - L) - blocks.Sum(k => k.Max(b => b.W));
                double g = blocks.Count > 1 ? space / (blocks.Count - 1) : 0;
                if (g < 20) g = gap;
                double x = blocks.Count > 1 ? L : (L + R) / 2 - blocks[0].Max(b => b.W) / 2;
                foreach (List<IBox> block in blocks)
                {
                    double w = block.Max(b => b.W);
                    foreach (IBox b in block) MoveBox(b, x + (w - b.W) / 2, b.Y);
                    x += w + g;
                }
                break;
            }
            case AlignOp.DistributeV:
            {
                // Items sharing a centre line (side by side) move as one block, so they stay lined up.
                // First outline top stays at T, last outline bottom at B; gaps are measured from the bottom of each
                // block's lowest label (if any) to the top of the next block's outlines.
                List<List<IBox>> blocks = SameCentre(items, b => b.Y + b.H / 2);
                double space = (B - T) - blocks.Sum(k => k.Max(b => b.H)) - blocks.Take(blocks.Count - 1).Sum(BlockExtraBelow);
                double g = blocks.Count > 1 ? space / (blocks.Count - 1) : 0;
                if (g < 20) g = gap;
                double y = blocks.Count > 1 ? T : (T + B) / 2 - blocks[0].Max(b => b.H) / 2;
                foreach (List<IBox> block in blocks)
                {
                    double h = block.Max(b => b.H);
                    double extra = BlockExtraBelow(block);
                    foreach (IBox b in block) MoveBox(b, b.X, y + (h - b.H) / 2);
                    y += h + extra + g;
                }
                break;
            }
            case AlignOp.Row:
            {
                List<IBox> sorted = items.OrderBy(b => b.X).ToList();
                double tallest = sorted.Max(b => b.H);
                double mid = within is not null ? T + tallest / 2 : (T + B) / 2;
                double x = L;
                foreach (IBox b in sorted) { MoveBox(b, SnapV(x), mid - b.H / 2); x += b.W + gap; }
                break;
            }
            case AlignOp.Column:
            {
                List<IBox> sorted = items.OrderBy(b => b.Y).ToList();
                double widest = sorted.Max(b => b.W);
                double mid = within is not null ? L + widest / 2 : (L + R) / 2;
                double y = T;
                foreach (IBox b in sorted) { MoveBox(b, mid - b.W / 2, SnapV(y)); y += b.H + TextLayout.ExtraBelow(b) + gap; }
                break;
            }
        }

        if (within is not null)
        {
            if (op is AlignOp.Row or AlignOp.Column) FitGroup(within); else GrowGroup(within);
        }
        else
        {
            GroupTree tree = Tree;
            foreach (Group g in items.OfType<IGroupable>().Select(tree.Parent).OfType<Group>().Distinct().ToList())
                GrowGroup(g);
        }
    }

    /// <summary>
    /// Items grouped by a shared centre line (<paramref name="centre"/> equal to within half a pixel), in order along
    /// that axis. Distribute spaces these blocks rather than single items, so items already lined up stay lined up.
    /// </summary>
    static List<List<IBox>> SameCentre(List<IBox> items, Func<IBox, double> centre)
    {
        List<List<IBox>> blocks = new List<List<IBox>>();
        foreach (IBox b in items.OrderBy(centre))
        {
            if (blocks.Count > 0 && Math.Abs(centre(blocks[^1][0]) - centre(b)) < 0.5) blocks[^1].Add(b);
            else blocks.Add(new List<IBox> { b });
        }
        return blocks;
    }

    /// <summary>Space a block of side-by-side items takes under its tallest outline: the lowest label or description
    /// hanging below any of them (see <see cref="TextLayout.ExtraBelow"/>), measured from the block's centred bottom edge.</summary>
    static double BlockExtraBelow(List<IBox> block)
    {
        double h = block.Max(b => b.H);
        return Math.Max(0, block.Max(b => TextLayout.ExtraBelow(b) - (h - b.H) / 2));
    }

    public void AlignSelection(AlignOp op) => Align(op, Selection.Select(FindBox).Where(b => b is not null).Cast<IBox>().ToList(), null);

    public void AlignInGroup(Group g, AlignOp op) => Align(op, Members(g).Cast<IBox>().ToList(), g);

    /// <summary>
    /// Swaps the two selected shapes' places: each shape (type, label, icon, colour, size, look, properties, link)
    /// moves to where the other one was, centred on its spot. The places keep their connectors, their group and,
    /// when both shapes are steps, their step number, so the flow stays as drawn and only what happens at each spot
    /// changes. With an outcome involved the step number goes with the shape (outcomes have none).
    /// </summary>
    public void SwapSelection()
    {
        if (SwapPair() is not (Node a, Node b)) { view.ShowToast("Select exactly two shapes to swap"); return; }
        Change();
        bool keepSteps = a.Kind.IsStep && b.Kind.IsStep;
        Node atA = TakePlace(b, a, keepSteps);
        Node atB = TakePlace(a, b, keepSteps);
        Diagram.Nodes[Diagram.Nodes.IndexOf(a)] = atA;
        Diagram.Nodes[Diagram.Nodes.IndexOf(b)] = atB;
        GroupTree tree = Tree;
        foreach (Group g in new[] { atA, atB }.Select(n => tree.Parent(n)).OfType<Group>().Distinct().ToList())
            GrowGroup(g);
        view.ShowToast($"Swapped “{a.Label}” and “{b.Label}”");
    }
}
