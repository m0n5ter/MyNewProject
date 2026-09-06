using CommunityToolkit.Mvvm.ComponentModel;

namespace MyNewProject.ViewModels.Shapes;

/// <summary>
/// This one IS partial: it has a property of its own, so the generator has work
/// to do. The extra property is also the point - the templates differ not only
/// in how they look, but in what data they show.
/// </summary>
internal sealed partial class RectangleViewModel : ShapeViewModel
{
    [ObservableProperty]
    public partial double CornerRadius { get; set; }

    public override string Kind => "Rectangle";

    public override double Area => Width * Height;
}
