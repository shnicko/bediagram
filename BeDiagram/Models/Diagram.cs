using System.Text.Json.Serialization;
using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models;

/// <summary>
/// A diagram document. Saved to disk as JSON (*.diagram.json).
/// Nodes are typed (see <see cref="NodeTypes"/>) and carry free-form properties so that a
/// future audit-rule builder can interpret the same file without a format change.
/// </summary>
public class Diagram : IJsonOnDeserialized
{
    public string Schema { get; set; } = "bediagram/1";
    public string Name { get; set; } = "Untitled";
    public DateTime Modified { get; set; } = DateTime.UtcNow;
    /// <summary>Style given to new connectors in this diagram.</summary>
    public string DefaultConnector { get; set; } = EdgeStyles.Elbow;
    /// <summary>Look (weight, pattern, colour, arrows, label direction) given to new connectors.</summary>
    public Edge DefaultLook { get; set; } = new();
    /// <summary>Optional fixed page (paper size). Size "none" = open canvas.</summary>
    public PageSetup Page { get; set; } = new();
    /// <summary>Draw each shape's step number in a small badge. The order itself is always kept
    /// (<see cref="Node.Step"/>); this only decides whether it is shown. False is left out of the file.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool ShowSteps { get; set; }
    public List<Group> Groups { get; set; } = new();
    public List<Node> Nodes { get; set; } = new();
    public List<Edge> Edges { get; set; } = new();
    public List<TextBox> TextBoxes { get; set; } = new();

    /// <summary>Read-only shim: files saved before text boxes had a "comments" list. They load as comment text boxes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<LegacyComment>? Comments { get; set; }

    void IJsonOnDeserialized.OnDeserialized()
    {
        if (Comments is null) return;
        foreach (LegacyComment c in Comments)
        {
            TextBox t = TextBox.NewComment();
            t.Id = c.Id; t.Text = c.Text; t.X = c.X; t.Y = c.Y; t.W = c.W; t.H = c.H; t.GroupId = c.GroupId;
            t.Fill = t.BorderColor = c.Color switch { "blue" => "sky", "green" => "mint", "pink" => "rose", _ => "yellow" };
            TextBoxes.Add(t);
            foreach (Edge e in Edges.Where(e => e.From == c.Id || e.To == c.Id)) TextBox.ApplyPinLook(e);
        }
        Comments = null;
    }
}
