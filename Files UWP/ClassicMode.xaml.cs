using Windows.ApplicationModel.Core;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Files.Filesystem;
using Files.Interacts;

namespace Files
{

    public sealed partial class ClassicMode : Page
    {
        public static Page ClassicView;
        public ItemViewModel<ClassicMode> instanceViewModel;
        public Interaction<ClassicMode> instanceInteraction;
        public ClassicMode()
        {
            this.InitializeComponent();
            instanceViewModel = new ItemViewModel<ClassicMode>();
            instanceInteraction = new Interaction<ClassicMode>();
            var CoreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            Window.Current.SetTitleBar(DragArea);
            CoreTitleBar.ExtendViewIntoTitleBar = true;
            var titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonBackgroundColor = Color.FromArgb(0, 255, 255, 255);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            titleBar.ButtonHoverBackgroundColor = Color.FromArgb(75, 10, 10, 10);
            ClassicView = ClassicModePage;
            instanceViewModel.AddItemsToCollectionAsync(@"C:\", ClassicView);
        }

        
        private void DirectoryView_Expanding(Microsoft.UI.Xaml.Controls.TreeView sender, Microsoft.UI.Xaml.Controls.TreeViewExpandingEventArgs args)
        {
            if (args.Node.HasUnrealizedChildren)
            {
                instanceViewModel.FillTreeNode(args.Item, sender);
            }
        }
    }
}
