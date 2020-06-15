using Files.Filesystem;
using Files.Interacts;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Files
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Search : Page
    {
        public ItemViewModel AssociatedViewModel = null;
        public Interaction AssociatedInteractions = null;
        public Search()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (AssociatedViewModel == null && AssociatedInteractions == null)
            {
                AssociatedViewModel = App.CurrentInstance.ViewModel;
                AssociatedInteractions = App.CurrentInstance.InteractionOperations;
            }

            AssociatedViewModel.IsQuickSearch = false;
        }

        private void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Interaction.OpenFile((e.ClickedItem as ListedItem).ItemPath, false);
        }

        private void PathLink_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}