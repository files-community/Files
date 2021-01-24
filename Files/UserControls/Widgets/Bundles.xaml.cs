using Files.ViewModels.Bundles;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.Widgets
{
    public sealed partial class Bundles : UserControl
    {
        public BundlesViewModel ViewModel { get; private set; }

        public Bundles()
        {
            this.InitializeComponent();

            this.ViewModel = new BundlesViewModel();
            this.DataContext = ViewModel;
        }
    }
}