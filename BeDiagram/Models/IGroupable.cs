namespace BeDiagram.Models;

/// <summary>Items that can sit inside a group.</summary>
public interface IGroupable : IBox
{
    string? GroupId { get; set; }
}
