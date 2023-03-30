using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace Files.App.Views.Properties
{
	public sealed partial class CompatibilityPage : BasePropertiesPage
	{
		public CompatibilityViewModel CompatibilityViewModel { get; set; }

		public CompatibilityPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = e.Parameter as MainPropertiesPage.PropertyNavParam;
			if (np.navParameter is ListedItem listedItem)
				CompatibilityViewModel = new(listedItem);

			base.OnNavigatedTo(e);
		}

		protected override void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			base.Properties_Loaded(sender, e);

			CompatibilityViewModel?.GetCompatibilityOptions();
		}

		public override Task<bool> SaveChangesAsync()
		{
			if (CompatibilityViewModel is not null)
				return Task.FromResult(CompatibilityViewModel.SetCompatibilityOptions());

			return Task.FromResult(false);
		}

		public override void Dispose()
		{
		}
	}
}
