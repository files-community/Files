using Files.App.DataModels;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Filesystem;
using Files.App.ViewModels.Properties;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace Files.App.Views.Properties
{
	public sealed partial class SecurityPage : BasePropertiesPage
	{
		private SecurityViewModel SecurityViewModel { get; set; }

		private object _parameter;

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

			_parameter = e.Parameter;

			base.OnNavigatedTo(e);
		}

		private void OpenSecurityAdvancedPageButton_Click(object sender, RoutedEventArgs e)
		{
			Frame?.Navigate(typeof(SecurityAdvancedPage), _parameter);
		}

		public async override Task<bool> SaveChangesAsync()
			=> await Task.FromResult(SecurityViewModel is null || SecurityViewModel.SaveChangedAccessControlList());

		public override void Dispose()
		{
		}
	}
}
