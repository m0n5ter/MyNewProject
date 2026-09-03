using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyNewProject.ViewModels;

/// <summary>
/// Root view model. It owns two child view models and switches between them.
/// The children know nothing about each other - all the wiring lives here.
///
/// Lesson 6: [NotifyPropertyChangedFor] replaces the hand-written
/// OnPropertyChanged(nameof(IsLoginVisible)) inside the setter.
/// </summary>
internal partial class MainViewModel : ObservableObject
{
    // Note the trade-off: [ObservableProperty] always generates a PUBLIC setter,
    // while the hand-written property had a private one. If that matters,
    // keep writing the property by hand - the generator is not always the answer.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoginVisible))]
    private bool _isLoggedIn;

    [ObservableProperty]
    private string _currentUser = string.Empty;

    public MainViewModel()
    {
        Login = new LoginViewModel();
        Login.LoginSucceeded += OnLoginSucceeded;

        TodoList = new TodoListViewModel();
    }

    public LoginViewModel Login { get; }

    public TodoListViewModel TodoList { get; }

    public bool IsLoginVisible => !IsLoggedIn;

    private void OnLoginSucceeded(string userName)
    {
        CurrentUser = userName;
        IsLoggedIn = true;
    }

    [RelayCommand]
    private void Logout()
    {
        IsLoggedIn = false;
        CurrentUser = string.Empty;
        Login.Reset();

        // The next user must not see the previous user's items.
        TodoList.Clear();
    }
}
