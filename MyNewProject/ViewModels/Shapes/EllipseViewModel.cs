namespace MyNewProject.ViewModels.Shapes;

/// <summary>
/// Note: NOT partial. There is no [ObservableProperty] here, so there is nothing
/// for the generator to add. 'partial' is written where the generator works,
/// not "just in case".
/// </summary>
internal sealed class EllipseViewModel : ShapeViewModel
{
    public override string Kind => "Ellipse";

    // Pi * a * b
    public override double Area => Math.PI * (Width / 2) * (Height / 2);
}
