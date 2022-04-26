using Files.ViewModels.Previews;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.FilePreviews
{
    public sealed partial class ImagePreview : UserControl
    {
        public ImagePreview(ImagePreviewViewModel model)
        {
            ViewModel = model;
            InitializeComponent();
        }

        private ImagePreviewViewModel ViewModel { get; set; }
    }
}