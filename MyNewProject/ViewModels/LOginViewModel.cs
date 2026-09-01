using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyNewProject.ViewModels;

/// <summary>
/// Child view model: knows only about the login form.
/// It does not know what happens after a successful login -
/// it just raises an event, and the parent (MainViewModel) decides.
/// </summary>
internal class LoginViewModel : ObservableObject
{
    private string _userName = "admin";
    private string _password = "123";
    private bool _isLoggingIn;
    private bool _hasError;

    public event Action<string>? LoginSucceeded;

    public LoginViewModel()
    {
        LoginCommand = new RelayCommand(async () => await LoginAsync(), CanLogin);
    }

    public string UserName
    {
        get => _userName;
        set
        {
            if (SetProperty(ref _userName, value))
            {
                HasError = false;
                LoginCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
            {
                HasError = false;
                LoginCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsLoggingIn
    {
        get => _isLoggingIn;
        set
        {
            if (SetProperty(ref _isLoggingIn, value))
                LoginCommand.NotifyCanExecuteChanged();
        }
    }

    public bool HasError
    {
        get => _hasError;
        set => SetProperty(ref _hasError, value);
    }

    public RelayCommand LoginCommand { get; }

    public void Reset()
    {
        UserName = string.Empty;
        Password = string.Empty;
        HasError = false;
    }

    private bool CanLogin() =>
        !IsLoggingIn && !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password);

    private async Task LoginAsync()
    {
        try
        {
            IsLoggingIn = true;
            HasError = false;

            // Pretend we are calling a server.
            await Task.Delay(1500);

            if (UserName == "admin" && Password == "123")
            {
                var user = UserName;
                Reset();
                LoginSucceeded?.Invoke(user);
            }
            else
            {
                HasError = true;
            }
        }
        catch
        {
            // TODO log or something
            HasError = true;
        }
        finally
        {
            IsLoggingIn = false;
        }
    }
}
