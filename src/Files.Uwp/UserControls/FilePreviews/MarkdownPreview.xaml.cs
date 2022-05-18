using Files.Uwp.ViewModels.Previews;
using Windows.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Files.Uwp.UserControls.FilePreviews
{
    public sealed partial class MarkdownPreview : UserControl
    {
        public MarkdownPreview(MarkdownPreviewViewModel model)
        {
            ViewModel = model;
            InitializeComponent();
        }

        private MarkdownPreviewViewModel ViewModel { get; set; }
    }
}