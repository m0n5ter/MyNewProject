using System.Text.Json.Serialization;

namespace MyNewProject.ViewModels.Shapes;

/// <summary>
/// Note: NOT partial. There is no [ObservableProperty] here, so there is nothing
/// for the generator to add. 'partial' is written where the generator works,
/// not "just in case".
/// </summary>
internal sealed class EllipseViewModel : ShapeViewModel
{
    // [JsonIgnore] has to be repeated on the override: System.Text.Json reads the
    // attribute off the most derived declaration, so the one on the abstract member
    // in ShapeViewModel does not carry over. Verified, not assumed.
    [JsonIgnore]
    public override string Kind => "Ellipse";

    // Pi * a * b
    [JsonIgnore]
    public override double Area => Math.PI * (Width / 2) * (Height / 2);
}
