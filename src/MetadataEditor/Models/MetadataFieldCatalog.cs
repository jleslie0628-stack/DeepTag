namespace MetadataEditor.Models;

public static class MetadataFieldCatalog
{
    public static IReadOnlyList<MetadataFieldDefinition> All { get; } =
    [
        new()
        {
            Id = "CreationTime",
            Label = "Created",
            Section = MetadataSection.Filesystem,
            Kind = MetadataFieldKind.DateTime
        },
        new()
        {
            Id = "LastWriteTime",
            Label = "Modified",
            Section = MetadataSection.Filesystem,
            Kind = MetadataFieldKind.DateTime
        },
        new()
        {
            Id = "LastAccessTime",
            Label = "Accessed",
            Section = MetadataSection.Filesystem,
            Kind = MetadataFieldKind.DateTime
        },
        new()
        {
            Id = "DateTimeOriginal",
            Label = "Date Taken",
            Section = MetadataSection.Media,
            Kind = MetadataFieldKind.DateTime,
            ExifTagName = "DateTimeOriginal"
        },
        new()
        {
            Id = "Make",
            Label = "Camera Make",
            Section = MetadataSection.Media,
            Kind = MetadataFieldKind.Text,
            ExifTagName = "Make"
        },
        new()
        {
            Id = "Model",
            Label = "Camera Model",
            Section = MetadataSection.Media,
            Kind = MetadataFieldKind.Text,
            ExifTagName = "Model"
        },
        new()
        {
            Id = "LensModel",
            Label = "Lens",
            Section = MetadataSection.Media,
            Kind = MetadataFieldKind.Text,
            ExifTagName = "LensModel"
        },
        new()
        {
            Id = "ISO",
            Label = "ISO",
            Section = MetadataSection.Media,
            Kind = MetadataFieldKind.ReadOnlyText,
            ExifTagName = "ISO"
        },
        new()
        {
            Id = "FNumber",
            Label = "F-Stop",
            Section = MetadataSection.Media,
            Kind = MetadataFieldKind.ReadOnlyText,
            ExifTagName = "FNumber"
        },
        new()
        {
            Id = "ExposureTime",
            Label = "Shutter Speed",
            Section = MetadataSection.Media,
            Kind = MetadataFieldKind.ReadOnlyText,
            ExifTagName = "ExposureTime"
        },
        new()
        {
            Id = "GPSLatitude",
            Label = "GPS Latitude",
            Section = MetadataSection.Media,
            Kind = MetadataFieldKind.Text,
            ExifTagName = "GPSLatitude"
        },
        new()
        {
            Id = "GPSLongitude",
            Label = "GPS Longitude",
            Section = MetadataSection.Media,
            Kind = MetadataFieldKind.Text,
            ExifTagName = "GPSLongitude"
        }
    ];
}
