namespace MyNewProject.ViewModels;

internal class LOginViewModel : ViewModelBase
{
    private bool _loggedIn;
    private string _password;
    private string _userName;
    private bool _isLoggingIn;

    public string UserName
    {
        get => _userName;
        set
        {
            if (SetField(ref _userName, value))
            {
                OnPropertyChanged(nameof(HelloText));
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
                OnPropertyChanged(nameof(HelloText));
                LoginCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string HelloText => $"Hello {UserName}! Password: {Password}";

    public bool LoggedIn
    {
        get => _loggedIn;
        set => SetField(ref _loggedIn, value);
    }

    public UiCommand LoginCommand { get; }

    public LOginViewModel()
    {
        LoginCommand = new UiCommand(async () => await Login(), () => !string.IsNullOrEmpty(UserName) && !string.IsNullOrEmpty(Password));
    }

    private async Task Login()
    {
        try
        {
            IsLoggingIn = true;
            await Task.Delay(3000);
            LoggedIn = UserName == "admin" && Password == "123";
            UserName = Password = string.Empty;
        }
        catch
        {
            // TODO log or something
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    public bool IsLoggingIn
    {
        get => _isLoggingIn;
        set => SetField(ref _isLoggingIn, value);
    }
}