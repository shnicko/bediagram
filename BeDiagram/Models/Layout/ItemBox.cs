namespace BeDiagram.Models.Layout;

/// <summary>
/// One row of a list or IPO node, as a box a connector can attach to. <see cref="Id"/> is the node's id (so code that
/// joins items by id joins the node) and <see cref="ItemId"/> the row's. Connectors leave and arrive on the row's
/// left or right side only (see <see cref="Geometry.Route(IBox, IBox, string, string?, string?, double)"/>).
/// </summary>
public class ItemBox : IBox
{
    public ItemBox(string nodeId, string itemId, double x, double y, double w, double h)
    {
        Id = nodeId; ItemId = itemId; X = x; Y = y; W = w; H = h;
    }

    public string Id { get; }
    public string ItemId { get; }
    public double X { get; set; }
    public double Y { get; set; }
    public double W { get; set; }
    public double H { get; set; }
}
