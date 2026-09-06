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
/// </summary>
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
    public abstract string Kind { get; }

    /// <summary>The same question, a different formula in every shape - polymorphism.</summary>
    public abstract double Area { get; }

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
