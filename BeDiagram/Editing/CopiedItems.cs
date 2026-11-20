using BeDiagram.Models;

namespace BeDiagram.Editing;

/// <summary>
/// Shapes, text boxes and groups copied with Ctrl+C (or Ctrl+X), with the connectors between them: copies of the
/// items as they were, keeping their ids so pasting can tell which connector and group goes with which item. Groups
/// come with everything inside them. Kept by the <c>ItemClipboard</c> service, so they can be pasted into any tab or
/// window; <see cref="DiagramEditor.PasteItems"/> gives the pasted copies new ids.
/// </summary>
public class CopiedItems
{
    /// <summary>The items in the order Duplicate has always used: each selected item, followed by what's inside it.</summary>
    public List<IBox> Boxes { get; } = new List<IBox>();

    /// <summary>The connectors whose two ends were both copied, in the diagram's order.</summary>
    public List<Edge> Edges { get; } = new List<Edge>();

    /// <summary>What was selected (the pasted copies of these are selected after pasting).</summary>
    public List<string> SelectedIds { get; } = new List<string>();

    public int Count
    {
        get
        {
            return Boxes.Count;
        }
    }

    /// <summary>A copy of <paramref name="b"/> that keeps its id (Copy() makes a new one).</summary>
    public static IBox Keep(IBox b)
    {
        if (b is Node n)
        {
            Node c = n.Copy();
            c.Id = n.Id;
            return c;
        }
        if (b is TextBox t)
        {
            TextBox c = t.Copy();
            c.Id = t.Id;
            return c;
        }
        if (b is Group g)
        {
            Group c = g.Copy();
            c.Id = g.Id;
            return c;
        }
        return b;
    }

    /// <summary>A copy of <paramref name="e"/> with the same id, ends, label, look and route.</summary>
    public static Edge Keep(Edge e)
    {
        Edge c = new Edge();
        c.Id = e.Id;
        c.From = e.From;
        c.To = e.To;
        c.FromItem = e.FromItem;
        c.ToItem = e.ToItem;
        c.Label = e.Label;
        c.LabelPos = e.LabelPos;
        c.CopyLookFrom(e);
        c.CopyRouteFrom(e);
        return c;
    }
}
