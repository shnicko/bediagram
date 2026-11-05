namespace BeDiagram.Models;

/// <summary>Items that can sit inside a group: shapes, text boxes and other groups (the Composite component).</summary>
public interface IGroupable : IBox
{
    /// <summary>Id of the group directly holding this item, or null at the top level.</summary>
    string? GroupId { get; set; }
}
