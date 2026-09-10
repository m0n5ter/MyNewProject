using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyNewProject.ViewModels;

/// <summary>
/// Everything the signed-in user sees: a header and a set of tabs.
///
/// It lives exactly one session - created on login, thrown away on logout.
/// That is why nothing here has to be reset by hand: the next user gets
/// a brand new object, not a cleaned up old one.
/// </summary>
internal partial class WorkspaceViewModel : ViewModelBase
{
    // userName comes from the login, the rest comes from the container.
    // ActivatorUtilities.CreateInstance mixes the two - see App.ConfigureServices.
    public WorkspaceViewModel(string userName, TodoListViewModel todoList, ImageViewModel image)
    {
        UserName = userName;
        Tabs = [todoList, image];
        SelectedTab = todoList;
    }

    public override string Title => "Workspace";

    public string UserName { get; }

    // A collection of ViewModelBase, not of two concrete types: adding a third tab
    // means adding an item here and writing a DataTemplate for it. Nowhere else.
    public ObservableCollection<ViewModelBase> Tabs { get; }

    [ObservableProperty]
    public partial ViewModelBase SelectedTab { get; set; }

    // The child does not sign the user out on its own - it reports that it was asked to.
    // The parent decides what that means. Same shape as LoginViewModel.LoginSucceeded.
    public event Action? LogoutRequested;

    [RelayCommand]
    private void Logout() => LogoutRequested?.Invoke();
}
