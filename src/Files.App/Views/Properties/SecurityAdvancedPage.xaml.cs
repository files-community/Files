using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace Files.App.Views.Properties
{
	public sealed partial class SecurityAdvancedPage : BasePropertiesPage
	{
		public SecurityViewModel? SecurityViewModel { get; set; }

		public SecurityAdvancedPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var args = (MainPropertiesPage.PropertyNavParam)e.Parameter;

			if (args.navParameter is ListedItem listedItem)
				SecurityViewModel = new SecurityViewModel(listedItem);
			else if (args.navParameter is DriveItem driveitem)
				SecurityViewModel = new SecurityViewModel(driveitem);

			base.OnNavigatedTo(e);
		}

		public async override Task<bool> SaveChangesAsync()
		{
			return SecurityViewModel is null || SecurityViewModel.SaveChangedAccessControlList();
		}

		public override void Dispose()
		{
		}
	}
}
