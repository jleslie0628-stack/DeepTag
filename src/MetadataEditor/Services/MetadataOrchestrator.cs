using System.Globalization;
using System.Text.RegularExpressions;
using MetadataEditor.Models;

namespace MetadataEditor.Services;

public sealed class MetadataOrchestrator(
    ExifMetadataService exifService,
    FileSystemMetadataService fileSystemService)
{
    private static readonly Regex GpsCoordinatePattern = new(
        @"^-?\d+(?:\.\d+)?(?:\s*(?:°|deg)?\s*\d+(?:\.\d+)?['′]?\s*(?:\d+(?:\.\d+)?"")?)?$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<FileMetadataSnapshot> LoadSnapshotAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("File not found.", filePath);
        }

        var filesystem = fileSystemService.ReadFilesystem(filePath);
        var (exifTags, hasMediaMetadata) = await exifService.ReadAsync(filePath, cancellationToken);

        return new FileMetadataSnapshot
        {
            FilePath = filesystem.FilePath,
            CreationTime = filesystem.CreationTime,
            LastWriteTime = filesystem.LastWriteTime,
            LastAccessTime = filesystem.LastAccessTime,
            ExifTags = exifTags,
            HasMediaMetadata = hasMediaMetadata
        };
    }

    public IReadOnlyList<MetadataChange> BuildChanges(
        FileMetadataSnapshot original,
        IReadOnlyDictionary<string, string?> editedValues)
    {
        var changes = new List<MetadataChange>();

        foreach (var definition in MetadataFieldCatalog.All.Where(d => d.IsEditable))
        {
            if (definition.Section == MetadataSection.Media && !original.HasMediaMetadata)
            {
                continue;
            }

            if (!editedValues.TryGetValue(definition.Id, out var editedValue))
            {
                continue;
            }

            var originalValue = GetFieldValue(original, definition);
            var normalizedOriginal = NormalizeValue(definition, originalValue);
            var normalizedEdited = NormalizeValue(definition, editedValue);

            if (!string.Equals(normalizedOriginal, normalizedEdited, StringComparison.Ordinal))
            {
                ValidateValue(definition, normalizedEdited);
                changes.Add(new MetadataChange
                {
                    FieldId = definition.Id,
                    Label = definition.Label,
                    OriginalValue = normalizedOriginal,
                    NewValue = normalizedEdited
                });
            }
        }

        return changes;
    }

    public async Task ApplyChangesAsync(
        FileMetadataSnapshot original,
        IReadOnlyList<MetadataChange> changes,
        CancellationToken cancellationToken = default)
    {
        if (changes.Count == 0)
        {
            return;
        }

        var filesystemChanges = changes.Where(c => FileSystemMetadataService.IsFilesystemField(c.FieldId)).ToList();
        var mediaChanges = changes.Where(c => !FileSystemMetadataService.IsFilesystemField(c.FieldId)).ToList();

        if (mediaChanges.Count > 0)
        {
            if (!original.HasMediaMetadata)
            {
                throw new InvalidOperationException("This file does not contain editable media metadata.");
            }

            var tagValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var change in mediaChanges)
            {
                var definition = MetadataFieldCatalog.All.First(d => d.Id == change.FieldId);
                if (string.IsNullOrWhiteSpace(definition.ExifTagName))
                {
                    continue;
                }

                var formatted = FormatForExifTool(definition, change.NewValue);
                tagValues[definition.ExifTagName] = formatted;

                if (definition.Id == "DateTimeOriginal")
                {
                    tagValues["CreateDate"] = formatted;
                    tagValues["MediaCreateDate"] = formatted;
                }
            }

            await exifService.WriteAsync(original.FilePath, tagValues, cancellationToken);
        }

        if (filesystemChanges.Count > 0)
        {
            try
            {
                fileSystemService.ApplyChanges(original.FilePath, filesystemChanges);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException(
                    "Access denied while updating filesystem dates. Close any apps using this file or run as administrator.",
                    ex);
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException(
                    "Unable to update filesystem dates because the file is in use.",
                    ex);
            }
        }
    }

    public static string? GetFieldValue(FileMetadataSnapshot snapshot, MetadataFieldDefinition definition)
    {
        return definition.Id switch
        {
            "CreationTime" => snapshot.CreationTime.ToString("O"),
            "LastWriteTime" => snapshot.LastWriteTime.ToString("O"),
            "LastAccessTime" => snapshot.LastAccessTime.ToString("O"),
            _ when definition.ExifTagName == "DateTimeOriginal" =>
                ExifMetadataService.FindTagValue(snapshot.ExifTags, "DateTimeOriginal")
                ?? ExifMetadataService.FindTagValue(snapshot.ExifTags, "CreateDate")
                ?? ExifMetadataService.FindTagValue(snapshot.ExifTags, "MediaCreateDate"),
            _ when definition.ExifTagName is not null =>
                ExifMetadataService.FindTagValue(snapshot.ExifTags, definition.ExifTagName),
            _ => null
        };
    }

    private static string NormalizeValue(MetadataFieldDefinition definition, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (definition.Kind == MetadataFieldKind.DateTime &&
            DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed.ToString("O");
        }

        return value.Trim();
    }

    private static void ValidateValue(MetadataFieldDefinition definition, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException($"{definition.Label} cannot be empty.");
        }

        if (definition.Kind == MetadataFieldKind.DateTime &&
            !DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind, out _))
        {
            throw new FormatException($"{definition.Label} must be a valid date and time.");
        }

        if (definition.Id is "GPSLatitude" or "GPSLongitude")
        {
            if (!GpsCoordinatePattern.IsMatch(value) && !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            {
                throw new FormatException($"{definition.Label} must be a valid coordinate.");
            }
        }
    }

    private static string FormatForExifTool(MetadataFieldDefinition definition, string value)
    {
        if (definition.Kind == MetadataFieldKind.DateTime &&
            DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed.ToString("yyyy:MM:dd HH:mm:ss");
        }

        return value;
    }
}
