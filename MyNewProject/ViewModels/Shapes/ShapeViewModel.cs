using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyNewProject.ViewModels.Shapes;

/// <summary>
/// Base class of every shape on the canvas.
///
/// Everything the shapes have in common lives here: position, size and the
/// ability to move. Everything that differs is abstract - the derived class
/// answers for itself. That is the whole design of this lesson.
///
/// 'abstract partial' is a normal combination: abstract because nobody draws
/// a "shape in general", partial because [ObservableProperty] generates code
/// into this very class.
///
/// Lesson 8: the [JsonDerivedType] attributes are what makes the polymorphic
/// collection survive a round trip - each element is written with a "$type"
/// discriminator, so an ellipse is still an ellipse after loading. It is a fair
/// question whether the persistence format belongs in a view model at all; the
/// honest alternative is a DTO inside JsonSceneStorage. Two attributes are cheaper
/// here, and the moment the file format stops matching the view models, we move.
/// </summary>
[JsonDerivedType(typeof(EllipseViewModel), "ellipse")]
[JsonDerivedType(typeof(RectangleViewModel), "rectangle")]
internal abstract partial class ShapeViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    public partial double X { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    public partial double Y { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Area), nameof(Description))]
    public partial double Width { get; set; } = 80;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Area), nameof(Description))]
    public partial double Height { get; set; } = 60;

    /// <summary>Human readable name. Every shape knows its own.</summary>
    // Computed members are not state: writing them to the file would only add noise.
    [JsonIgnore]
    public abstract string Kind { get; }

    /// <summary>The same question, a different formula in every shape - polymorphism.</summary>
    [JsonIgnore]
    public abstract double Area { get; }

    [JsonIgnore]
    public string Description => $"{Kind} {Width:0}x{Height:0} at ({X:0}, {Y:0}), S = {Area:0}";

    /// <summary>
    /// Moves the shape by a delta. No bounds here on purpose: the shape does not
    /// know how big the canvas is - that rule belongs to the canvas view model.
    /// </summary>
    public void MoveBy(double dx, double dy)
    {
        X += dx;
        Y += dy;
    }
}
