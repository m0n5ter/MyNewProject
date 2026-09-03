using CommunityToolkit.Mvvm.ComponentModel;

namespace MyNewProject.ViewModels;

internal partial class ImageViewModel: ObservableObject
{
    [ObservableProperty] 
    public partial string ImageUrl { get; set; }

}