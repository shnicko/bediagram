namespace BeDiagram.Models.Layout;

/// <summary>One compartment of a class or entity: the divider line along its top and its rows.</summary>
public record ModelCompartment(double Top, double Bottom, List<ModelLine> Lines);
