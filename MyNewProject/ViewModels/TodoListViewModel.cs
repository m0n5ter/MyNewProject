using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyNewProject.ViewModels;

/// <summary>
/// Child view model that owns the collection of items.
///
/// Two kinds of notifications are used here:
/// 1. ObservableCollection.CollectionChanged - items added/removed;
/// 2. PropertyChanged of every item - a single item changed (IsDone).
/// The list subscribes to both to keep the counters up to date.
/// </summary>
internal class TodoListViewModel : ObservableObject
{
    private string _newTitle = string.Empty;
    private TodoItemViewModel? _selectedItem;

    public TodoListViewModel()
    {
        Items.CollectionChanged += OnItemsCollectionChanged;

        AddCommand = new RelayCommand(Add, () => !string.IsNullOrWhiteSpace(NewTitle));
        RemoveCommand = new RelayCommand<TodoItemViewModel>(Remove, item => item != null);
        ClearDoneCommand = new RelayCommand(ClearDone, () => DoneCount > 0);

        Items.Add(new TodoItemViewModel("Learn properties and bindings") { IsDone = true });
        Items.Add(new TodoItemViewModel("Learn nested view models"));
        Items.Add(new TodoItemViewModel("Learn collections"));
    }

    public ObservableCollection<TodoItemViewModel> Items { get; } = new();

    public string NewTitle
    {
        get => _newTitle;
        set
        {
            if (SetProperty(ref _newTitle, value))
                AddCommand.NotifyCanExecuteChanged();
        }
    }

    public TodoItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public int TotalCount => Items.Count;
    public int DoneCount => Items.Count(item => item.IsDone);
    public int ActiveCount => TotalCount - DoneCount;

    public RelayCommand AddCommand { get; }
    public RelayCommand<TodoItemViewModel> RemoveCommand { get; }
    public RelayCommand ClearDoneCommand { get; }

    public void Clear()
    {
        Items.Clear();
        NewTitle = string.Empty;
        SelectedItem = null;
    }

    private void Add()
    {
        Items.Add(new TodoItemViewModel(NewTitle.Trim()));
        NewTitle = string.Empty;
    }

    private void Remove(TodoItemViewModel? item)
    {
        if (item != null)
            Items.Remove(item);
    }

    private void ClearDone()
    {
        foreach (var done in Items.Where(item => item.IsDone).ToList())
            Items.Remove(done);
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // The parent listens to its children, and stops listening when they leave the list.
        foreach (var item in e.OldItems?.OfType<TodoItemViewModel>() ?? [])
            item.PropertyChanged -= OnItemPropertyChanged;

        foreach (var item in e.NewItems?.OfType<TodoItemViewModel>() ?? [])
            item.PropertyChanged += OnItemPropertyChanged;

        RaiseCountersChanged();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TodoItemViewModel.IsDone))
            RaiseCountersChanged();
    }

    private void RaiseCountersChanged()
    {
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(DoneCount));
        OnPropertyChanged(nameof(ActiveCount));
        ClearDoneCommand.NotifyCanExecuteChanged();
    }
}
