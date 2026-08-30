using CommunityToolkit.Mvvm.ComponentModel;

namespace MyNewProject.ViewModels;

/// <summary>
/// One item of the list. It is a full view model too:
/// it implements INotifyPropertyChanged, so the UI reacts to every change.
/// </summary>
internal class TodoItemViewModel : ObservableObject
{
    private string _title;
    private bool _isDone;

    public TodoItemViewModel(string title)
    {
        _title = title;
        CreatedAt = DateTime.Now;
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public bool IsDone
    {
        get => _isDone;
        set => SetProperty(ref _isDone, value);
    }

    public DateTime CreatedAt { get; }
}
