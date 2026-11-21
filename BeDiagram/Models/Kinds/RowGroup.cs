using BeDiagram.Models.Catalogue;

namespace BeDiagram.Models.Kinds;

/// <summary>One block of rows in the row editor window: a list's rows, an IPO section, or a class / entity
/// compartment. <see cref="Section"/> is what a new row is added to (null in a list); <see cref="Fields"/> are the
/// structured columns its rows have, besides the label and description.</summary>
public record RowGroup(RowSection? Section, string Title, ModelRowFields Fields, List<ListItem> Rows);
