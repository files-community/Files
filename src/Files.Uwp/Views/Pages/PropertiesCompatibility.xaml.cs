using Files.Filesystem;
using Files.ViewModels.Properties;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;

namespace Files.Views
{
    public sealed partial class PropertiesCompatibility : PropertiesTab
    {
        public CompatibilityProperties CompatibilityProperties { get; set; }

        public PropertiesCompatibility()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var np = e.Parameter as Views.Properties.PropertyNavParam;

            if (np.navParameter is ListedItem listedItem)
            {
                CompatibilityProperties = new CompatibilityProperties(listedItem);
            }

            base.OnNavigatedTo(e);
        }

        protected override void Properties_Loaded(object sender, RoutedEventArgs e)
        {
            base.Properties_Loaded(sender, e);

            if (CompatibilityProperties != null)
            {
                CompatibilityProperties.GetCompatibilityOptions();
            }
        }

        public override async Task<bool> SaveChangesAsync(ListedItem item)
        {
            if (CompatibilityProperties != null)
            {
                return await CompatibilityProperties.SetCompatibilityOptions();
            }
            return true;
        }

        public override void Dispose()
        {
        }
    }
}
