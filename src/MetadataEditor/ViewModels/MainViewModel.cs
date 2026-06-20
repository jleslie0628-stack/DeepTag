using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MetadataEditor.Models;
using MetadataEditor.Services;
using Microsoft.Win32;

namespace MetadataEditor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly MetadataOrchestrator _orchestrator;
    private FileMetadataSnapshot? _loadedSnapshot;

    public MainViewModel()
    {
        var runner = new ExifToolProcessRunner();
        _orchestrator = new MetadataOrchestrator(new ExifMetadataService(runner), new FileSystemMetadataService());
    }

    public ObservableCollection<FileItemViewModel> Files { get; } = [];
    public ObservableCollection<MetadataFieldViewModel> FilesystemFields { get; } = [];
    public ObservableCollection<MetadataFieldViewModel> MediaFields { get; } = [];

    [ObservableProperty]
    private FileItemViewModel? _selectedFile;

    [ObservableProperty]
    private string _statusMessage = "Add a file to begin editing metadata.";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _hasMediaMetadata;

    public bool HasPendingChanges => FilesystemFields.Any(f => f.IsChanged) || MediaFields.Any(f => f.IsChanged);

    partial void OnSelectedFileChanged(FileItemViewModel? value)
    {
        _ = LoadSelectedFileAsync();
    }

    [RelayCommand]
    private void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Select files to edit"
        };

        if (dialog.ShowDialog() == true)
        {
            AddFilePaths(dialog.FileNames);
        }
    }

    public void AddFilePaths(IEnumerable<string> paths)
    {
        foreach (var path in paths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (Files.Any(f => string.Equals(f.FilePath, path, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            Files.Add(new FileItemViewModel(path));
        }

        SelectedFile ??= Files.FirstOrDefault();
        StatusMessage = Files.Count == 0
            ? "Add a file to begin editing metadata."
            : $"{Files.Count} file(s) loaded.";
    }

    [RelayCommand(CanExecute = nameof(CanModifyCurrentFile))]
    private async Task ApplyAsync()
    {
        if (_loadedSnapshot is null || SelectedFile is null)
        {
            return;
        }

        try
        {
            var editedValues = CollectEditedValues();
            var changes = _orchestrator.BuildChanges(_loadedSnapshot, editedValues);
            if (changes.Count == 0)
            {
                StatusMessage = "No changes to apply.";
                return;
            }

            var summary = string.Join(Environment.NewLine, changes.Select(c =>
                $"{c.Label}: {FormatDisplayValue(c.OriginalValue)} -> {FormatDisplayValue(c.NewValue)}"));

            var confirm = MessageBox.Show(
                $"Apply the following changes to {SelectedFile.FileName}?{Environment.NewLine}{Environment.NewLine}{summary}",
                "Confirm Metadata Changes",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            IsBusy = true;
            await _orchestrator.ApplyChangesAsync(_loadedSnapshot, changes);
            await ReloadCurrentFileAsync();
            StatusMessage = $"Applied {changes.Count} change(s) to {SelectedFile.FileName}.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            MessageBox.Show(ex.Message, "Unable to Apply Changes", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            RefreshPendingState();
        }
    }

    [RelayCommand(CanExecute = nameof(CanModifyCurrentFile))]
    private void Revert()
    {
        foreach (var field in FilesystemFields.Concat(MediaFields))
        {
            field.ResetToOriginal();
        }

        StatusMessage = SelectedFile is null
            ? "Add a file to begin editing metadata."
            : $"Reverted unsaved changes for {SelectedFile.FileName}.";
        RefreshPendingState();
    }

    private bool CanModifyCurrentFile() => SelectedFile is not null && _loadedSnapshot is not null && !IsBusy;

    private async Task LoadSelectedFileAsync()
    {
        FilesystemFields.Clear();
        MediaFields.Clear();
        _loadedSnapshot = null;
        HasMediaMetadata = false;

        if (SelectedFile is null)
        {
            StatusMessage = "Add a file to begin editing metadata.";
            ApplyCommand.NotifyCanExecuteChanged();
            RevertCommand.NotifyCanExecuteChanged();
            return;
        }

        try
        {
            IsBusy = true;
            await ReloadCurrentFileAsync();
            StatusMessage = $"Loaded metadata for {SelectedFile.FileName}.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            MessageBox.Show(ex.Message, "Unable to Load Metadata", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            ApplyCommand.NotifyCanExecuteChanged();
            RevertCommand.NotifyCanExecuteChanged();
            RefreshPendingState();
        }
    }

    private async Task ReloadCurrentFileAsync()
    {
        if (SelectedFile is null)
        {
            return;
        }

        _loadedSnapshot = await _orchestrator.LoadSnapshotAsync(SelectedFile.FilePath);
        HasMediaMetadata = _loadedSnapshot.HasMediaMetadata;

        FilesystemFields.Clear();
        MediaFields.Clear();

        foreach (var definition in MetadataFieldCatalog.All)
        {
            if (definition.Section == MetadataSection.Media && !HasMediaMetadata)
            {
                continue;
            }

            var value = MetadataOrchestrator.GetFieldValue(_loadedSnapshot, definition);
            var field = new MetadataFieldViewModel(definition, value);
            field.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName is nameof(MetadataFieldViewModel.Value) or nameof(MetadataFieldViewModel.IsChanged))
                {
                    RefreshPendingState();
                }
            };

            if (definition.Section == MetadataSection.Filesystem)
            {
                FilesystemFields.Add(field);
            }
            else
            {
                MediaFields.Add(field);
            }
        }
    }

    private Dictionary<string, string?> CollectEditedValues()
    {
        return FilesystemFields
            .Concat(MediaFields)
            .ToDictionary(f => f.Id, f => (string?)f.Value, StringComparer.Ordinal);
    }

    private void RefreshPendingState()
    {
        OnPropertyChanged(nameof(HasPendingChanges));
        if (SelectedFile is not null)
        {
            SelectedFile.HasPendingChanges = HasPendingChanges;
        }

        ApplyCommand.NotifyCanExecuteChanged();
        RevertCommand.NotifyCanExecuteChanged();
    }

    private static string FormatDisplayValue(string value)
    {
        if (DateTime.TryParse(value, out var parsed))
        {
            return parsed.ToString("g");
        }

        return string.IsNullOrWhiteSpace(value) ? "(empty)" : value;
    }
}
