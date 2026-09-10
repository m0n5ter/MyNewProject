using MyNewProject.ViewModels.Shapes;

namespace MyNewProject.Services;

/// <summary>
/// Saving the scene is a service, not a job for a view model: it talks to the outside
/// world, it can fail, and in a test it must be replaceable by something that does not
/// touch the disk. The view model only asks for this interface in its constructor and
/// never learns which implementation it got.
/// </summary>
internal interface ISceneStorage
{
    Task SaveAsync(IReadOnlyList<ShapeViewModel> shapes, CancellationToken ct);

    Task<IReadOnlyList<ShapeViewModel>> LoadAsync(CancellationToken ct);
}
