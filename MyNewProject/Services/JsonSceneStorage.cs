using System.IO;
using System.Text.Json;
using MyNewProject.ViewModels.Shapes;

namespace MyNewProject.Services;

/// <summary>
/// Stores the scene as a JSON file next to the user's data.
///
/// The interesting part is polymorphism: the list is declared as ShapeViewModel, so
/// System.Text.Json writes a type discriminator for every element (see the
/// [JsonDerivedType] attributes on ShapeViewModel). An ellipse comes back an ellipse.
/// </summary>
internal sealed class JsonSceneStorage : ISceneStorage
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private readonly string _filePath;

    public JsonSceneStorage()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MyNewProject");

        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "scene.json");
    }

    public async Task SaveAsync(IReadOnlyList<ShapeViewModel> shapes, CancellationToken ct)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, shapes, Options, ct);
    }

    public async Task<IReadOnlyList<ShapeViewModel>> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath))
            return [];

        await using var stream = File.OpenRead(_filePath);

        var shapes = await JsonSerializer.DeserializeAsync<List<ShapeViewModel>>(stream, Options, ct);
        return shapes ?? [];
    }
}
