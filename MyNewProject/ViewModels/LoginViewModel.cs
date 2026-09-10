using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyNewProject.ViewModels;

/// <summary>
/// Child view model: knows only about the login form.
/// It does not know what happens after a successful login -
/// it just raises an event, and the parent (MainViewModel) decides.
///
/// Lesson 6: the hand-written IsLoggingIn flag is gone. The generated
/// LoginCommand is an IAsyncRelayCommand, so the view binds to
/// LoginCommand.IsRunning and the button disables itself while the task runs.
/// The error is a message, not a bool - same shape as in TodoListViewModel.
///
/// Lesson 8: it is a screen now, so it derives from ViewModelBase. Nothing else
/// changed - it still knows nothing about who shows it or what comes next.
/// </summary>
internal partial class LoginViewModel : ViewModelBase
{
    public override string Title => "Sign in";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _userName = "admin";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
    private string _password = "123";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public event Action<string>? LoginSucceeded;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // Generated hook: the setter of UserName calls this after the value changed.
    // This is how you attach a side effect to an [ObservableProperty].
    partial void OnUserNameChanged(string value) => ErrorMessage = null;

    partial void OnPasswordChanged(string value) => ErrorMessage = null;

    public void Reset()
    {
        UserName = string.Empty;
        Password = string.Empty;
        ErrorMessage = null;
    }

    // No '!IsLoggingIn' any more: an AsyncRelayCommand already reports
    // CanExecute == false while it is running.
    private bool CanLogin() =>
        !string.IsNullOrWhiteSpace(UserName) && !string.IsNullOrWhiteSpace(Password);

    [RelayCommand(CanExecute = nameof(CanLogin))]
    private async Task LoginAsync()
    {
        ErrorMessage = null;

        try
        {
            // Pretend we are calling a server.
            await Task.Delay(500);

            if (UserName == "admin" && Password == "123")
            {
                var user = UserName;
                Reset();
                LoginSucceeded?.Invoke(user);
            }
            else
            {
                ErrorMessage = "Wrong user name or password";
            }
        }
        catch (Exception ex)
        {
            // TODO log or something
            ErrorMessage = ex.Message;
        }
    }
}
