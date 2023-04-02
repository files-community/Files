using Files.App.DataModels;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace Files.App.Views.Properties
{
	public sealed partial class SecurityPage : BasePropertiesPage
	{
		public SecurityViewModel SecurityViewModel { get; set; }

		public SecurityPage()
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = (PropertiesPageArguments)e.Parameter;
			if (np.Parameter is ListedItem listedItem)
				SecurityViewModel = new(listedItem);
			else if (np.Parameter is DriveItem driveitem)
				SecurityViewModel = new(driveitem);

			base.OnNavigatedTo(e);
		}

		public async override Task<bool> SaveChangesAsync()
			=> Task.FromResult(SecurityViewModel is null || SecurityViewModel.SaveChangedAccessControlList());

		public override void Dispose()
		{
		}
	}
}
