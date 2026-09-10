using CommunityToolkit.Mvvm.ComponentModel;

namespace MyNewProject.ViewModels;

/// <summary>
/// Root view model. After lesson 8 it is a router and nothing else: it holds
/// the screen that is currently shown and decides which one comes next.
///
/// Note what is NOT here any more:
///   - IsLoggedIn / IsLoginVisible and the two visibility converters behind them;
///   - TodoList.Clear() in Logout - the whole workspace is a new object now;
///   - 'new' anywhere. Every dependency is asked for in the constructor.
/// </summary>
internal partial class MainViewModel : ViewModelBase
{
    private readonly LoginViewModel _login;

    // Not a WorkspaceViewModel: we need a NEW one per session, and the user name
    // is only known at run time. A factory is the answer to both - see 4.5 of the lesson.
    private readonly Func<string, WorkspaceViewModel> _workspaceFactory;

    public MainViewModel(LoginViewModel login, Func<string, WorkspaceViewModel> workspaceFactory)
    {
        _login = login;
        _workspaceFactory = workspaceFactory;
        _login.LoginSucceeded += OnLoginSucceeded;

        CurrentViewModel = _login;
    }

    public override string Title => "Todo";

    // The whole navigation state of the application. One assignment switches the screen;
    // the view finds the matching DataTemplate by the runtime type all by itself.
    [ObservableProperty]
    public partial ViewModelBase CurrentViewModel { get; private set; }

    private void OnLoginSucceeded(string userName)
    {
        var workspace = _workspaceFactory(userName);
        workspace.LogoutRequested += OnLogoutRequested;
        CurrentViewModel = workspace;
    }

    private void OnLogoutRequested()
    {
        // Unsubscribing is not optional: the event holds a reference to this handler,
        // so the old workspace - and the whole object graph behind it - would stay alive.
        if (CurrentViewModel is WorkspaceViewModel old)
            old.LogoutRequested -= OnLogoutRequested;

        _login.Reset();
        CurrentViewModel = _login;
    }
}
