using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MediaDateRenamer;

public sealed class MediaItem : INotifyPropertyChanged
{
    private string _originalPath = string.Empty;
    private string _detectedDateText = string.Empty;
    private string _sourceUsed = string.Empty;
    private string _newFileName = string.Empty;
    private string _status = string.Empty;
    private bool _selected;

    public string OriginalPath
    {
        get => _originalPath;
        set => SetField(ref _originalPath, value);
    }

    public string DetectedDateText
    {
        get => _detectedDateText;
        set => SetField(ref _detectedDateText, value);
    }

    public string SourceUsed
    {
        get => _sourceUsed;
        set => SetField(ref _sourceUsed, value);
    }

    public string NewFileName
    {
        get => _newFileName;
        set => SetField(ref _newFileName, value);
    }

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public bool Selected
    {
        get => _selected;
        set => SetField(ref _selected, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}