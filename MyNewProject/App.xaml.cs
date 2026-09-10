using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MyNewProject.Services;
using MyNewProject.ViewModels;

namespace MyNewProject
{
    /// <summary>
    /// Interaction logic for App.xaml
    ///
    /// This class is the composition root: the one place in the application that is
    /// allowed to know which implementation stands behind which interface, and how
    /// long every object lives. Nothing else touches the container - a view model
    /// that asks IServiceProvider for something is a service locator, not DI.
    /// </summary>
    public partial class App : Application
    {
        private readonly ServiceProvider _services;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            _services = services.BuildServiceProvider(new ServiceProviderOptions
            {
                // Both checks on purpose: ValidateOnBuild reports a missing registration
                // at startup instead of half an hour later, when the user finally opens
                // the rare screen that needed it.
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Services: interface -> implementation. Swapping JsonSceneStorage for an
            // in-memory one in tests is this single line, and nothing else changes.
            services.AddSingleton<ISceneStorage, JsonSceneStorage>();

            // View models. The lifetime is a decision, not a default.
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<LoginViewModel>();

            // Transient, and never injected into the singleton MainViewModel directly:
            // a singleton holding a transient turns it into a singleton (captive
            // dependency). They are created per session through the factory below.
            services.AddTransient<TodoListViewModel>();
            services.AddTransient<ImageViewModel>();

            // A screen with a run-time argument. The container can only build an object
            // out of REGISTERED dependencies, and the user name is not one of them -
            // it appears at run time. ActivatorUtilities takes the arguments we pass and
            // resolves the rest from the container.
            services.AddSingleton<Func<string, WorkspaceViewModel>>(sp =>
                userName => ActivatorUtilities.CreateInstance<WorkspaceViewModel>(sp, userName));

            services.AddSingleton<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var window = _services.GetRequiredService<MainWindow>();
            window.DataContext = _services.GetRequiredService<MainViewModel>();
            window.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Every IDisposable the container created is released here.
            _services.Dispose();
            base.OnExit(e);
        }
    }
}
