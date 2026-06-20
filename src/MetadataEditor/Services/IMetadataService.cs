using MetadataEditor.Models;

namespace MetadataEditor.Services;

public interface IMetadataService
{
    Task<FileMetadataSnapshot> ReadAsync(string filePath, CancellationToken cancellationToken = default);
    Task ApplyChangesAsync(string filePath, IReadOnlyList<MetadataChange> changes, CancellationToken cancellationToken = default);
}
