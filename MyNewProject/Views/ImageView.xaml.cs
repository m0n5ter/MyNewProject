using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MyNewProject.ViewModels;
using MyNewProject.ViewModels.Shapes;

namespace MyNewProject.Views
{
    /// <summary>
    /// Interaction logic for ImageView.xaml
    ///
    /// Yes, there is code behind here, and it is still MVVM. Neither handler holds
    /// a rule: they translate a mouse gesture into a call on the view model.
    /// The rule itself - "a shape may not leave the canvas" - lives in
    /// ImageViewModel.MoveShape and is testable without a window.
    /// </summary>
    public partial class ImageView : UserControl
    {
        public ImageView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Thumb has already captured the mouse and calculated the delta for us.
        /// </summary>
        private void OnShapeDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (sender is Thumb { DataContext: ShapeViewModel shape }
                && DataContext is ImageViewModel viewModel)
            {
                viewModel.MoveShape(shape, e.HorizontalChange, e.VerticalChange);
            }
        }

        /// <summary>
        /// The Thumb marks the bubbling MouseLeftButtonDown as handled, so the
        /// ListBoxItem never sees the click and selection would never happen.
        /// The tunnelling Preview event runs from the root down and gets here first.
        /// Note that e.Handled is deliberately left alone.
        /// </summary>
        private void OnShapePreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item)
                item.IsSelected = true;
        }
    }
}
