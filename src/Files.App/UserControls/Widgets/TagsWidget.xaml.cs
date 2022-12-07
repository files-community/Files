using Files.Backend.ViewModels.Widgets.FileTagsWidget;
using Microsoft.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.Widgets
{
    public sealed partial class TagsWidget : UserControl
    {
        public FileTagsWidgetViewModel ViewModel
        {
            get => (FileTagsWidgetViewModel)DataContext;
            set => ViewModel = value;
        }

        public TagsWidget()
        {
            this.InitializeComponent();

            ViewModel = new(null);
        }
    }
}
