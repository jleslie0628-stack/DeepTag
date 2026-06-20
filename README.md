# Metadata Editor

A native Windows desktop app for editing file metadata:

- **Filesystem dates** for any file type (Created, Modified, Accessed)
- **Embedded photo/video metadata** via [ExifTool](https://exiftool.org/) (date taken, camera make/model, lens, GPS, and more)

## Requirements

- Windows 10 or later
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## First-Time Setup

ExifTool is bundled at build time from `tools/exiftool/`. If that folder is missing (for example after a fresh clone), run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/download-exiftool.ps1
```

## Validate

```powershell
powershell -ExecutionPolicy Bypass -File tools/validate-metadata.ps1
```

This checks filesystem date edits, PNG/JPEG/MP4 metadata behavior, and that ExifTool is copied into the build output.

```powershell
cd C:\Users\Jeff\Projects\metadata-editor
dotnet build
dotnet run --project src/MetadataEditor
```

## Publish

```powershell
dotnet publish src/MetadataEditor -c Release -r win-x64 --self-contained
```

The published output includes the bundled ExifTool executable under `tools/exiftool/`.

## Usage

1. Click **Add Files** or drag files into the window.
2. Select a file in the left panel.
3. Edit filesystem and/or media metadata fields in the center panel.
4. Click **Apply** to save changes (you will be asked to confirm first).
5. Click **Revert** to discard unsaved edits.

Changed fields are highlighted until applied or reverted.

## Third-Party Components

This app bundles **ExifTool** by Phil Harvey for reading and writing embedded metadata. ExifTool is invoked as an external process and is licensed under the GPL/Artistic License. See [exiftool.org](https://exiftool.org/) for details.

## License

MIT (application source). Bundled ExifTool remains under its own license.
