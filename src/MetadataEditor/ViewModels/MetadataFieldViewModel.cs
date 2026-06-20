using CommunityToolkit.Mvvm.ComponentModel;
using MetadataEditor.Models;

namespace MetadataEditor.ViewModels;

public partial class MetadataFieldViewModel : ObservableObject
{
    public MetadataFieldViewModel(MetadataFieldDefinition definition, string? initialValue)
    {
        Definition = definition;
        _value = initialValue ?? string.Empty;
        OriginalValue = _value;
    }

    public MetadataFieldDefinition Definition { get; }

    public string Id => Definition.Id;
    public string Label => Definition.Label;
    public MetadataSection Section => Definition.Section;
    public MetadataFieldKind Kind => Definition.Kind;
    public bool IsEditable => Definition.IsEditable;

    public string OriginalValue { get; private set; }

    [ObservableProperty]
    private string _value;

    public bool IsChanged => !string.Equals(OriginalValue, Value, StringComparison.Ordinal);

    partial void OnValueChanged(string value) => OnPropertyChanged(nameof(IsChanged));

    public void ResetToOriginal()
    {
        Value = OriginalValue;
    }

    public void Commit(string committedValue)
    {
        OriginalValue = committedValue;
        Value = committedValue;
        OnPropertyChanged(nameof(IsChanged));
    }
}
