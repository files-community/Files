using Files.ViewModels.Bundles;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.UserControls.Widgets
{
    public sealed partial class Bundles : UserControl
    {
        public BundlesViewModel ViewModel 
        {
            get => (BundlesViewModel)DataContext;
            private set => DataContext = value;
        }

        public Bundles()
        {
            this.InitializeComponent();

            this.ViewModel = new BundlesViewModel();
        }

        private void GridView_DragEnter(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            Debug.WriteLine("GridView_DragEnter");
        }

        private void GridView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            Debug.WriteLine("GridView_DragItemsCompleted");
        }

        private void GridView_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
        {
            Debug.WriteLine("GridView_DragItemsStarting");
        }

        private void GridView_DragLeave(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            Debug.WriteLine("GridView_DragLeave");
        }

        private void GridView_DragOver(object sender, Windows.UI.Xaml.DragEventArgs e)
        {
            Debug.WriteLine("GridView_DragOver");
        }

        private void GridView_DragStarting(Windows.UI.Xaml.UIElement sender, Windows.UI.Xaml.DragStartingEventArgs args)
        {
            Debug.WriteLine("GridView_DragStarting");
        }
    }
}