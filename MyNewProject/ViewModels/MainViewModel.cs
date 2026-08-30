using MyNewProject.Mvvm;

namespace MyNewProject.ViewModels;

/// <summary>
/// Root view model. It owns two child view models and switches between them.
/// The children know nothing about each other - all the wiring lives here.
/// </summary>
internal class MainViewModel : ViewModelBase
{
    private bool _isLoggedIn;
    private string _currentUser = string.Empty;

    public MainViewModel()
    {
        Login = new LoginViewModel();
        Login.LoginSucceeded += OnLoginSucceeded;

        TodoList = new TodoListViewModel();

        LogoutCommand = new UiCommand(Logout);
    }

    public LoginViewModel Login { get; }

    public TodoListViewModel TodoList { get; }

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        private set
        {
            if (SetField(ref _isLoggedIn, value))
                OnPropertyChanged(nameof(IsLoginVisible));
        }
    }

    public bool IsLoginVisible => !IsLoggedIn;

    public string CurrentUser
    {
        get => _currentUser;
        private set => SetField(ref _currentUser, value);
    }

    public UiCommand LogoutCommand { get; }

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
