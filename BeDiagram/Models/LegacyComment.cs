namespace BeDiagram.Models;

public class LegacyComment
{
    public string Id { get; set; } = "";
    public string Text { get; set; } = "";
    public string Color { get; set; } = "yellow";
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; } = 200;
    public double H { get; set; } = 110;
    public string? GroupId { get; set; }
}
