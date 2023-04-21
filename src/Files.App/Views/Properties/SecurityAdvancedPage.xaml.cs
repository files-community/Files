using Files.App.DataModels;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace Files.App.Views.Properties
{
	public sealed partial class SecurityAdvancedPage : BasePropertiesPage
	{
		private SecurityAdvancedViewModel SecurityAdvancedViewModel { get; set; }

		public SecurityAdvancedPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = (PropertiesPageNavigationParameter)e.Parameter;
			if (np.Parameter is ListedItem listedItem)
				SecurityAdvancedViewModel = new(listedItem, np.Window);
			else if (np.Parameter is DriveItem driveitem)
				SecurityAdvancedViewModel = new(driveitem, np.Window);

			base.OnNavigatedTo(e);
		}

		public async override Task<bool> SaveChangesAsync()
			=> await Task.FromResult(SecurityAdvancedViewModel is null || SecurityAdvancedViewModel.SaveChangedAccessControlList());

		public override void Dispose()
		{
		}
	}
}
