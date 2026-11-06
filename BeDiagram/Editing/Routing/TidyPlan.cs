using BeDiagram.Models;

namespace BeDiagram.Editing.Routing;

/// <summary>What <see cref="ConnectorTidier.Plan"/> found: the new routes, and counts for the message to the user.</summary>
public class TidyPlan
{
    /// <summary>Connectors to re-route: new from / to sides and middle-segment offset.</summary>
    public List<(Edge Edge, string From, string To, double Bend)> Routes { get; } = new List<(Edge Edge, string From, string To, double Bend)>();
    /// <summary>Connectors that needed tidying: through a shape or text, through their own ends' text, or on a side
    /// another connector uses the other way.</summary>
    public int Crossing { get; set; }
    /// <summary>Of those, how many get a route that misses every shape and text box.</summary>
    public int Cleared { get; set; }
}
