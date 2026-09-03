using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyNewProject.ViewModels;

internal partial class TodoListViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCommand))]
    public partial string NewTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial TodoItemViewModel? SelectedItem { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }

    [ObservableProperty]
    public partial bool SimulateError { get; set; }

    public TodoListViewModel()
    {
        Items.CollectionChanged += OnItemsCollectionChanged;
    }

    public ObservableCollection<TodoItemViewModel> Items { get; } = new();

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public int TotalCount => Items.Count;

    public int DoneCount => Items.Count(item => item.IsDone);
    
    public int ActiveCount => TotalCount - DoneCount;

    public void Clear()
    {
        Items.Clear();
        NewTitle = string.Empty;
        SelectedItem = null;
        ErrorMessage = null;
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task LoadAsync(CancellationToken ct)
    {
        ErrorMessage = null;

        // Items.Clear();

        try
        {
            await Task.Delay(5000, ct);

            if (SimulateError)
                throw new InvalidOperationException("The server is down. Try again later.");

            Items.Add(new TodoItemViewModel("Learn properties and bindings") { IsDone = true });
            Items.Add(new TodoItemViewModel("Learn nested view models"));
            Items.Add(new TodoItemViewModel("Learn collections"));
        }
        catch (OperationCanceledException)
        {
            // Cancellation is not a failure - the user asked for it. Always a separate branch.
            ErrorMessage = "Loading cancelled.";
        }
        catch (Exception ex)
        {
            // Inside 'async void' this exception would have killed the application.
            // Inside 'async Task' behind an async command it is just a value we can show.
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAdd))]
    private void Add()
    {
        Items.Add(new TodoItemViewModel(NewTitle.Trim()));
        NewTitle = string.Empty;
    }

    private bool CanAdd() => !string.IsNullOrWhiteSpace(NewTitle);

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private void Remove(TodoItemViewModel? item)
    {
        if (item != null)
            Items.Remove(item);
    }

    private bool CanRemove(TodoItemViewModel? item) => item is not null;

    [RelayCommand(CanExecute = nameof(CanClearDone))]
    private void ClearDone()
    {
        foreach (var done in Items.Where(item => item.IsDone).ToList())
            Items.Remove(done);
    }

    private bool CanClearDone() => DoneCount > 0;

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
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
