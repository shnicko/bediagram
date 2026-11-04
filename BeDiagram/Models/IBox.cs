namespace BeDiagram.Models;

public interface IBox
{
    string Id { get; }
    double X { get; set; }
    double Y { get; set; }
    double W { get; set; }
    double H { get; set; }
}
