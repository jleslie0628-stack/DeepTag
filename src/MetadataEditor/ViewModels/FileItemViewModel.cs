using CommunityToolkit.Mvvm.ComponentModel;

namespace MetadataEditor.ViewModels;

public partial class FileItemViewModel : ObservableObject
{
    public FileItemViewModel(string filePath)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);
    }

    public string FilePath { get; }
    public string FileName { get; }

    [ObservableProperty]
    private bool _hasPendingChanges;
}
