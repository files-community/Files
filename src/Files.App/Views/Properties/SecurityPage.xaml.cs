using CommunityToolkit.Mvvm.Input;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace Files.App.Views.Properties
{
	public sealed partial class SecurityPage : BasePropertiesPage
	{
		public SecurityViewModel? SecurityViewModel { get; set; }

		public IRelayCommand OpenSecurityAdvancedPageCommand { get; set; }

		public SecurityPage()
		{
			InitializeComponent();

			OpenSecurityAdvancedPageCommand = new RelayCommand(OpenSecurityAdvancedPage);
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = (MainPropertiesPage.PropertyNavParam)e.Parameter;

			if (np.navParameter is ListedItem listedItem)
				SecurityViewModel = new SecurityViewModel(listedItem);
			else if (np.navParameter is DriveItem driveitem)
				SecurityViewModel = new SecurityViewModel(driveitem);

			base.OnNavigatedTo(e);
		}

		private void OpenSecurityAdvancedPage()
		{
			Frame.Navigate(
				typeof(SecurityAdvancedPage),
				new MainPropertiesPage.PropertyNavParam()
				{
					navParameter = SecurityViewModel.Item
				});
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
