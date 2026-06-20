namespace MetadataEditor.Models;

public sealed class FileMetadataSnapshot
{
    public required string FilePath { get; init; }
    public Dictionary<string, string> ExifTags { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTime CreationTime { get; init; }
    public DateTime LastWriteTime { get; init; }
    public DateTime LastAccessTime { get; init; }
    public bool HasMediaMetadata { get; init; }

    public FileMetadataSnapshot Clone() => new()
    {
        FilePath = FilePath,
        ExifTags = new Dictionary<string, string>(ExifTags, StringComparer.OrdinalIgnoreCase),
        CreationTime = CreationTime,
        LastWriteTime = LastWriteTime,
        LastAccessTime = LastAccessTime,
        HasMediaMetadata = HasMediaMetadata
    };
}
