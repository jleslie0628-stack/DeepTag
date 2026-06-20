using System.Diagnostics;
using System.Text;
using System.Text.Json;
using MetadataEditor.Models;

namespace MetadataEditor.Services;

public sealed class ExifToolLocator
{
    public static string GetExifToolPath()
    {
        var bundled = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "exiftool", "exiftool.exe");
        if (File.Exists(bundled))
        {
            return bundled;
        }

        var bundledAlt = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "exiftool", "exiftool(-k).exe");
        if (File.Exists(bundledAlt))
        {
            return bundledAlt;
        }

        return "exiftool.exe";
    }
}

public sealed class ExifToolProcessRunner
{
    public async Task<(int ExitCode, string StdOut, string StdErr)> RunAsync(
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var exifToolPath = ExifToolLocator.GetExifToolPath();
        var startInfo = new ProcessStartInfo
        {
            FileName = exifToolPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start ExifTool.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, await stdoutTask, await stderrTask);
    }
}

public sealed class ExifMetadataService(ExifToolProcessRunner runner)
{
    private static readonly HashSet<string> MediaIndicatorTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "DateTimeOriginal",
        "Make",
        "Model",
        "LensModel",
        "ISO",
        "FNumber",
        "ExposureTime",
        "GPSLatitude",
        "GPSLongitude",
        "CreateDate",
        "MediaCreateDate",
        "QuickTime:CreateDate"
    };

    public async Task<(Dictionary<string, string> Tags, bool HasMediaMetadata)> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var (exitCode, stdout, stderr) = await runner.RunAsync(["-json", "-G1", "-a", filePath], cancellationToken);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr)
                ? "ExifTool failed to read metadata."
                : stderr.Trim());
        }

        if (string.IsNullOrWhiteSpace(stdout))
        {
            return (new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), false);
        }

        using var document = JsonDocument.Parse(stdout);
        if (document.RootElement.ValueKind != JsonValueKind.Array || document.RootElement.GetArrayLength() == 0)
        {
            return (new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), false);
        }

        var fileObject = document.RootElement[0];
        var tags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var hasMediaMetadata = false;

        foreach (var property in fileObject.EnumerateObject())
        {
            if (property.NameEquals("SourceFile") || property.NameEquals("ExifToolVersion"))
            {
                continue;
            }

            var value = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => property.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => property.Value.ToString()
            };

            tags[property.Name] = value;

            var shortName = property.Name.Contains(':')
                ? property.Name[(property.Name.LastIndexOf(':') + 1)..]
                : property.Name;

            if (MediaIndicatorTags.Contains(shortName))
            {
                hasMediaMetadata = true;
            }
        }

        return (tags, hasMediaMetadata);
    }

    public async Task WriteAsync(
        string filePath,
        IReadOnlyDictionary<string, string> tagValues,
        CancellationToken cancellationToken = default)
    {
        if (tagValues.Count == 0)
        {
            return;
        }

        var arguments = new List<string> { "-overwrite_original" };
        foreach (var (tagName, value) in tagValues)
        {
            arguments.Add($"-{tagName}={value}");
        }

        arguments.Add(filePath);

        var (exitCode, _, stderr) = await runner.RunAsync(arguments, cancellationToken);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr)
                ? "ExifTool failed to write metadata."
                : stderr.Trim());
        }
    }

    public static string? FindTagValue(IReadOnlyDictionary<string, string> tags, string tagName)
    {
        if (tags.TryGetValue(tagName, out var direct))
        {
            return direct;
        }

        foreach (var (key, value) in tags)
        {
            if (key.EndsWith($":{tagName}", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }

        return null;
    }
}
