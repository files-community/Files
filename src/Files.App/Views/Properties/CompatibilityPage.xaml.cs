using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace Files.App.Views.Properties
{
	public sealed partial class CompatibilityPage : PropertiesTab
	{
		public CompatibilityProperties CompatibilityProperties { get; set; }

		public CompatibilityPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = e.Parameter as Views.Properties.MainPage.PropertyNavParam;

			if (np.navParameter is ListedItem listedItem)
			{
				CompatibilityProperties = new CompatibilityProperties(listedItem);
			}

			base.OnNavigatedTo(e);
		}

		protected override void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			base.Properties_Loaded(sender, e);

			if (CompatibilityProperties is not null)
			{
				CompatibilityProperties.GetCompatibilityOptions();
			}
		}

		public override Task<bool> SaveChangesAsync()
		{
			if (CompatibilityProperties is not null)
				return Task.FromResult(CompatibilityProperties.SetCompatibilityOptions());

			return Task.FromResult(false);
		}

		public override void Dispose()
		{
		}
	}
}
