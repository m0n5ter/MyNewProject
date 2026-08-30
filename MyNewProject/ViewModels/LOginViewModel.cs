using MyNewProject.Mvvm;

namespace MyNewProject.ViewModels;

/// <summary>
/// Child view model: knows only about the login form.
/// It does not know what happens after a successful login -
/// it just raises an event, and the parent (MainViewModel) decides.
/// </summary>
internal class LoginViewModel : ViewModelBase
{
    private string _userName = string.Empty;
    private string _password = string.Empty;
    private bool _isLoggingIn;
    private bool _hasError;

    public event Action<string>? LoginSucceeded;

    public LoginViewModel()
    {
        LoginCommand = new UiCommand(async () => await LoginAsync(), CanLogin);
    }

    public string UserName
    {
        get => _userName;
        set
        {
            if (SetField(ref _userName, value))
            {
                HasError = false;
                LoginCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (SetField(ref _password, value))
            {
                HasError = false;
                LoginCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsLoggingIn
    {
        get => _isLoggingIn;
        set
        {
            if (SetField(ref _isLoggingIn, value))
                LoginCommand.RaiseCanExecuteChanged();
        }
    }

    public bool HasError
    {
        get => _hasError;
        set => SetField(ref _hasError, value);
    }

    public UiCommand LoginCommand { get; }

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
