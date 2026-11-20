namespace BeDiagram.Models.Kinds;

/// <summary>Which structured fields a row of a class or entity section uses, so the side panel shows only those:
/// visibility (+ − # ~), data type (or return type), multiplicity, and primary / foreign key.</summary>
public record ModelRowFields(bool Visibility, bool DataType, bool Multiplicity, bool Keys);
