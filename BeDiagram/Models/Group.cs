using System.Text.Json.Serialization;
using BeDiagram.Models.Styles;

namespace BeDiagram.Models;

/// <summary>A coloured container. Members are the nodes/comments whose GroupId points here.</summary>
public class Group : IBox
{
    public const double Header = 32;
    public const double Pad = 20;

    public string Id { get; set; } = Node.NewId("g");
    public string Title { get; set; } = "Group";
    public string Color { get; set; } = "blue";
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 360;
    public double H { get; set; } = 220;
    /// <summary>Name of another saved diagram (without extension) holding this group's more detailed flow. Shows a link badge.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; }
    /// <summary>Tinted background (true) or no fill, just the outline (false).</summary>
    public bool Filled { get; set; } = true;
    /// <summary>Outline pattern: a <see cref="LineLook"/> pattern key ("solid", "dashed", "dotted", ...).</summary>
    public string Border { get; set; } = LineLook.Solid;
    /// <summary>Where the title goes: "top" or "bottom" (see <see cref="GroupLook"/>).</summary>
    public string HeaderPos { get; set; } = GroupLook.Top;
    /// <summary>"band" = title on a tinted strip inside the group; "text" = plain bold title
    /// (inside at the top, or centred under the box when the title is at the bottom).</summary>
    public string HeaderStyle { get; set; } = GroupLook.Band;
}
