using CommunityToolkit.Mvvm.ComponentModel;

namespace MyNewProject.ViewModels;

/// <summary>
/// One item of the list. It is a full view model too:
/// it implements INotifyPropertyChanged, so the UI reacts to every change.
/// </summary>
internal partial class TodoItemViewModel : ObservableObject
{
    public TodoItemViewModel(string title)
    {
        Title = title;
        CreatedAt = DateTime.Now;
    }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial bool IsDone { get; set; }

    public DateTime CreatedAt { get; }
}
