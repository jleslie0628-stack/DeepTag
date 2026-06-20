using MetadataEditor.Models;

namespace MetadataEditor.Services;

public sealed class FileSystemMetadataService
{
    public FileMetadataSnapshot ReadFilesystem(string filePath)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists)
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        return new FileMetadataSnapshot
        {
            FilePath = info.FullName,
            CreationTime = info.CreationTime,
            LastWriteTime = info.LastWriteTime,
            LastAccessTime = info.LastAccessTime,
            HasMediaMetadata = false
        };
    }

    public void ApplyChanges(string filePath, IReadOnlyList<MetadataChange> changes)
    {
        foreach (var change in changes.Where(c => IsFilesystemField(c.FieldId)))
        {
            if (!DateTime.TryParse(change.NewValue, out var parsed))
            {
                throw new FormatException($"Invalid date/time for {change.Label}.");
            }

            switch (change.FieldId)
            {
                case "CreationTime":
                    File.SetCreationTime(filePath, parsed);
                    break;
                case "LastWriteTime":
                    File.SetLastWriteTime(filePath, parsed);
                    break;
                case "LastAccessTime":
                    File.SetLastAccessTime(filePath, parsed);
                    break;
            }
        }
    }

    public static bool IsFilesystemField(string fieldId) =>
        fieldId is "CreationTime" or "LastWriteTime" or "LastAccessTime";
}
