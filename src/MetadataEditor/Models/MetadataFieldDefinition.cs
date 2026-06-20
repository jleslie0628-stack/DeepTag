namespace MetadataEditor.Models;

public sealed class MetadataFieldDefinition
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required MetadataSection Section { get; init; }
    public required MetadataFieldKind Kind { get; init; }
    public string? ExifTagName { get; init; }

    public bool IsEditable => Kind != MetadataFieldKind.ReadOnlyText;
}
