namespace MetadataEditor.Models;

public sealed class MetadataChange
{
    public required string FieldId { get; init; }
    public required string Label { get; init; }
    public required string OriginalValue { get; init; }
    public required string NewValue { get; init; }
}
