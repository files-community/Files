using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.ViewModels.Properties;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.Graphics;
using static Files.App.Views.Properties.SecurityAdvancedPage;

namespace Files.App.Views.Properties
{
	public sealed partial class SecurityPage : BasePropertiesPage
	{
		public SecurityPage()
		{
			InitializeComponent();

			_isWinUI3 = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8);

			OpenSecurityAdvancedPageWindowCommand = new RelayCommand(OpenSecurityAdvancedPageWindow);
		}

		public SecurityViewModel? SecurityViewModel { get; set; }

		private AppWindow? propsView;

		private readonly bool _isWinUI3;

		public RelayCommand OpenSecurityAdvancedPageWindowCommand { get; set; }

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = (MainPropertiesPage.PropertyNavParam)e.Parameter;

			if (np.navParameter is ListedItem listedItem)
				SecurityViewModel = new SecurityViewModel(listedItem);
			else if (np.navParameter is DriveItem driveitem)
				SecurityViewModel = new SecurityViewModel(driveitem);

			base.OnNavigatedTo(e);
		}

		private void OpenSecurityAdvancedPageWindow()
		{
		}

		public async override Task<bool> SaveChangesAsync()
		{
			return SecurityViewModel is null || SecurityViewModel.SaveChangedAccessControlList();
		}

		private async void SecurityAdvancedPageWindow_Destroying(AppWindow sender, object args)
		{
			sender.Destroying -= SecurityAdvancedPageWindow_Destroying;
			propsView = null;

			if (SecurityViewModel is not null)
			{
				// Reload permissions when closing
				await DispatcherQueue.EnqueueAsync(() => SecurityViewModel.GetAccessControlList());
			}
		}

		public override void Dispose()
		{
		}
	}
}
