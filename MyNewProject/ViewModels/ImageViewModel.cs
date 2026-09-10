using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyNewProject.Services;
using MyNewProject.ViewModels.Shapes;

namespace MyNewProject.ViewModels;

/// <summary>
/// A tiny drawing canvas. One collection of ShapeViewModel holds both ellipses
/// and rectangles; nothing here asks "which type is it?" except the factory.
///
/// The view model owns the rules (where a shape may be, what the z-order is),
/// the view owns the looks (what an ellipse is drawn with) - see the implicit
/// DataTemplates in ImageView.xaml.
///
/// Lesson 8: the 'new' is gone. Saving the scene is somebody else's job, and this
/// class only says which job it needs done - ISceneStorage in the constructor.
/// Who implements it is decided once, in App.ConfigureServices.
/// </summary>
internal partial class ImageViewModel : ViewModelBase
{
    private static readonly Random Random = new();

    private readonly ISceneStorage _storage;

    public ImageViewModel(ISceneStorage storage)
    {
        _storage = storage;

        // Adding or removing a shape changes the status line.
        Shapes.CollectionChanged += (_, _) => OnPropertyChanged(nameof(StatusText));
    }

    public override string Title => "Image";

    public ObservableCollection<ShapeViewModel> Shapes { get; } = new();

    // The LOGICAL size of the document, not the pixel size of the control.
    // A view model must not know how the view stretched it on screen.
    public double CanvasWidth => 520;

    public double CanvasHeight => 300;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand), nameof(BringToFrontCommand))]
    public partial ShapeViewModel? SelectedShape { get; set; }

    // Lesson 6, unchanged rule: a failure is a state of the view model, not a MessageBox.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; private set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public string StatusText =>
        SelectedShape?.Description ?? $"Shapes: {Shapes.Count}. Nothing selected.";

    /// <summary>
    /// The rule "a shape may not leave the canvas" lives here, not in the view.
    /// The view only turns a mouse gesture into this call.
    /// </summary>
    public void MoveShape(ShapeViewModel shape, double dx, double dy)
    {
        shape.X = Math.Clamp(shape.X + dx, 0, CanvasWidth - shape.Width);
        shape.Y = Math.Clamp(shape.Y + dy, 0, CanvasHeight - shape.Height);
    }

    [RelayCommand]
    private void AddShape(string kind)
    {
        // This is a factory, and a factory is the ONE place allowed to know
        // about concrete types - somebody has to call new. Everywhere else
        // we only ever see a ShapeViewModel.
        ShapeViewModel shape = kind switch
        {
            "Ellipse" => new EllipseViewModel(),
            "Rectangle" => new RectangleViewModel { CornerRadius = 8 },
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown shape kind")
        };

        shape.X = Random.Next(0, (int)(CanvasWidth - shape.Width));
        shape.Y = Random.Next(0, (int)(CanvasHeight - shape.Height));

        Shapes.Add(shape);
        SelectedShape = shape;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Delete()
    {
        Shapes.Remove(SelectedShape!);
        SelectedShape = null;
    }

    /// <summary>
    /// Z-order on a Canvas is simply the position in the collection: later means
    /// higher. Move() raises a Move notification, so the container is reused -
    /// unlike Remove + Add, which would recreate it and drop the selection.
    /// </summary>
    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void BringToFront()
    {
        var shape = SelectedShape!;
        Shapes.Move(Shapes.IndexOf(shape), Shapes.Count - 1);
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct)
    {
        ErrorMessage = null;

        try
        {
            await _storage.SaveAsync(Shapes, ct);
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Saving cancelled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        ErrorMessage = null;

        try
        {
            var shapes = await _storage.LoadAsync(ct);

            // The collection object stays the same - the ListBox keeps its binding.
            Shapes.Clear();
            foreach (var shape in shapes)
                Shapes.Add(shape);

            SelectedShape = null;
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Loading cancelled.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void Clear()
    {
        Shapes.Clear();
        SelectedShape = null;
    }

    private bool HasSelection() => SelectedShape is not null;

    // The two-parameter flavour of the generated hook. It is exactly what you
    // need when a subscription has to follow the current value.
    partial void OnSelectedShapeChanged(ShapeViewModel? oldValue, ShapeViewModel? newValue)
    {
        if (oldValue is not null)
            oldValue.PropertyChanged -= OnSelectedShapePropertyChanged;

        if (newValue is not null)
            newValue.PropertyChanged += OnSelectedShapePropertyChanged;

        OnPropertyChanged(nameof(StatusText));
    }

    // ObservableCollection reports added and removed items, never a change
    // inside an item. Dragging changes X and Y - the status line has to be told.
    private void OnSelectedShapePropertyChanged(object? sender, PropertyChangedEventArgs e)
        => OnPropertyChanged(nameof(StatusText));
}
