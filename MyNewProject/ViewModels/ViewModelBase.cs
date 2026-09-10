using CommunityToolkit.Mvvm.ComponentModel;

namespace MyNewProject.ViewModels;

/// <summary>
/// Common ancestor of every screen.
///
/// It adds almost nothing - it exists as a TYPE, so that MainViewModel.CurrentViewModel
/// and WorkspaceViewModel.Tabs can hold any screen, while WPF picks the template by the
/// actual runtime type.
///
/// Lesson 8: an abstract class and not an IViewModel on purpose - implicit DataTemplates
/// are never looked up by interface, only by class and its base classes.
/// </summary>
internal abstract class ViewModelBase : ObservableObject
{
    /// <summary>Shown in the window caption or on a tab header. Every screen names itself.</summary>
    public abstract string Title { get; }
}
