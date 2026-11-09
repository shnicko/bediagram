using System.Text;
using BeDiagram.Models;
using BeDiagram.Models.Catalogue;
using BeDiagram.Models.Emphasis;
using BeDiagram.Models.Layout;
using BeDiagram.Models.Shapes;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models.Routing;

/// <summary>Connector labels: at most two lines, laid out inline (with a white backing), above or below the line.</summary>
public static class EdgeLabels
{
    public const int MaxEdgeLabelLines = 2;
    public const double EdgeLabelLineHeight = 15;
    /// <summary>Clear space between the connector and text set beside it.</summary>
    const double EdgeLabelGap = 4;


    /// <summary>A label's lines: at most two (anything after the second line break joins line two).</summary>
    public static List<string> EdgeLabelLines(string? label)
    {
        List<string> parts = (label ?? "").Replace("\r", "").Split('\n').Select(l => l.Trim()).ToList();
        while (parts.Count > 0 && parts[^1].Length == 0) parts.RemoveAt(parts.Count - 1);
        while (parts.Count > 0 && parts[0].Length == 0) parts.RemoveAt(0);
        if (parts.Count > MaxEdgeLabelLines)
            parts = new List<string> { parts[0], string.Join(" ", parts.Skip(1).Where(l => l.Length > 0)) };
        return parts;
    }

    /// <summary>The label text kept to two lines (used when editing finishes or text is pasted).</summary>
    public static string ClampEdgeLabel(string? label)
    {
        string raw = (label ?? "").Replace("\r", "");
        string[] parts = raw.Split('\n');
        if (parts.Length <= MaxEdgeLabelLines) return raw;
        return parts[0] + "\n" + string.Join(" ", parts.Skip(1).Select(l => l.Trim()).Where(l => l.Length > 0));
    }

    /// <summary>
    /// Where a connector label's lines go. When the text runs along the line, "above"/"below" stack the text on that
    /// side and a two-line inline label straddles the line (line one above, line two below). When the text runs
    /// across the line (level text on an up-down segment, or 90° labels), inline sits on the line in a box and
    /// above/below put it to the left/right of the line as the text reads.
    /// </summary>
    public static EdgeLabelLayout LayoutEdgeLabel(Edge e, EdgeGeom geo)
    {
        List<string> lines = EdgeLabelLines(e.Label);
        if (lines.Count == 0) lines.Add("");
        int n = lines.Count;
        double lh = EdgeLabelLineHeight;
        double textW = lines.Max(l => l.Length * 7.0);
        double d = (geo.LabelAngle - geo.SegAngle) * Math.PI / 180;
        bool along = Math.Abs(Math.Cos(d)) >= 0.5;
        LabelPlace place = e.LabelPlace;
        // Baselines: a 12px line's visual middle is ~4px above its baseline; cap height ~9, descent ~3.
        const double Ascent = 9, Descent = 3, Mid = 4;
        List<EdgeLabelLine> result = new List<EdgeLabelLine>();
        string anchor = "middle";
        Rect? box = null;
        double x = 0;
        double[] ys;

        if (along && place == LabelPlace.Inline && n == 2)
            ys = new[] { -EdgeLabelGap - Descent, EdgeLabelGap + Ascent };
        else if (along && place == LabelPlace.Above)
            ys = Enumerable.Range(0, n).Select(i => -EdgeLabelGap - Descent - (n - 1 - i) * lh).ToArray();
        else if (along && place == LabelPlace.Below)
            ys = Enumerable.Range(0, n).Select(i => EdgeLabelGap + Ascent + i * lh).ToArray();
        else
        {
            // Centred on the point (inline single line, or text across the line).
            ys = Enumerable.Range(0, n).Select(i => Mid + (i - (n - 1) / 2.0) * lh).ToArray();
            if (!along && place == LabelPlace.Above) { anchor = "end"; x = -EdgeLabelGap - 2; }
            else if (!along && place == LabelPlace.Below) { anchor = "start"; x = EdgeLabelGap + 2; }
            else
            {
                double bw = textW + 12, bh = n * lh + 5;
                box = new Rect(-bw / 2, -bh / 2, bw, bh);
            }
        }
        for (int i = 0; i < n; i++) result.Add(new EdgeLabelLine(lines[i], x, ys[i]));

        double left = anchor switch { "end" => x - textW, "start" => x, _ => x - textW / 2 };
        Rect hit = box ?? new Rect(left - 3, ys[0] - Ascent - 3, textW + 6, ys[^1] - ys[0] + Ascent + Descent + 6);
        return new EdgeLabelLayout(result, anchor, box, box is null, hit);
    }
}
