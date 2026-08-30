using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyNewProject.ViewModels;

/// <summary>
/// Root view model. It owns two child view models and switches between them.
/// The children know nothing about each other - all the wiring lives here.
/// </summary>
internal class MainViewModel : ObservableObject
{
    private bool _isLoggedIn;
    private string _currentUser = string.Empty;

    public MainViewModel()
    {
        Login = new LoginViewModel();
        Login.LoginSucceeded += OnLoginSucceeded;

        TodoList = new TodoListViewModel();

        LogoutCommand = new RelayCommand(Logout);
    }

    public LoginViewModel Login { get; }

    public TodoListViewModel TodoList { get; }

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        private set
        {
            if (SetProperty(ref _isLoggedIn, value))
                OnPropertyChanged(nameof(IsLoginVisible));
        }
    }

    public bool IsLoginVisible => !IsLoggedIn;

    public string CurrentUser
    {
        get => _currentUser;
        private set => SetProperty(ref _currentUser, value);
    }

    public RelayCommand LogoutCommand { get; }

    private void OnLoginSucceeded(string userName)
    {
        CurrentUser = userName;
        IsLoggedIn = true;
    }

    private void Logout()
    {
        IsLoggedIn = false;
        CurrentUser = string.Empty;
        Login.Reset();
    }
}
