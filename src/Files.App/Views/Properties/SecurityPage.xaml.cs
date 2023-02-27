using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Files.App.DataModels.NavigationControlItems;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.ViewModels.Properties;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
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

			OpenAdvancedPropertiesCommand = new RelayCommand(() => OpenAdvancedProperties());
		}

		public RelayCommand OpenAdvancedPropertiesCommand { get; set; }

		public SecurityViewModel SecurityViewModel { get; set; }

		private AppWindow? propsView;

		private readonly bool _isWinUI3;

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = e.Parameter as Properties.MainPropertiesPage.PropertyNavParam;

			if (np.navParameter is ListedItem listedItem)
			{
				SecurityViewModel = new SecurityViewModel(listedItem);
			}
			else if (np.navParameter is DriveItem driveitem)
			{
				SecurityViewModel = new SecurityViewModel(driveitem);
			}

			base.OnNavigatedTo(e);
		}

		public async override Task<bool> SaveChangesAsync()
		{
			return SecurityViewModel is null || SecurityViewModel.SetFilePermissions();
		}

		protected override void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			base.Properties_Loaded(sender, e);

			if (SecurityViewModel is not null)
			{
				SecurityViewModel.GetFilePermissions();
			}
		}

		public override void Dispose()
		{

		}

		private void OpenAdvancedProperties()
		{
			if (SecurityViewModel is null)
			{
				return;
			}

			if (_isWinUI3)
			{
				if (propsView is null)
				{
					var frame = new Frame();
					frame.RequestedTheme = ThemeHelper.RootTheme;
					frame.Navigate(typeof(SecurityAdvancedPage), new PropertiesPageNavigationArguments()
					{
						Item = SecurityViewModel.Item
					}, new SuppressNavigationTransitionInfo());

					// Initialize window
					var propertiesWindow = new WinUIEx.WindowEx()
					{
						IsMinimizable = false,
						IsMaximizable = false
					};
					var appWindow = propertiesWindow.AppWindow;

					// Set icon
					appWindow.SetIcon(FilePropertiesHelpers.LogoPath);

					// Set content
					propertiesWindow.Content = frame;
					if (frame.Content is SecurityAdvancedPage properties)
						properties.appWindow = appWindow;

					// Set min size
					propertiesWindow.MinWidth = 850;
					propertiesWindow.MinHeight = 550;

					// Set backdrop
					propertiesWindow.Backdrop = new WinUIEx.MicaSystemBackdrop();

					appWindow.TitleBar.ExtendsContentIntoTitleBar = true;

					// Set window buttons background to transparent
					appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
					appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

					appWindow.Title = string.Format("SecurityAdvancedPermissionsTitle".GetLocalizedResource(), SecurityViewModel.Item.Name);
					appWindow.Resize(new SizeInt32(850, 550));
					appWindow.Destroying += AppWindow_Destroying;
					appWindow.Show();

					propsView = appWindow;
				}
				else
				{
					propsView.Show(true);
				}
			}
			else
			{
				// Unsupported
			}
		}

		private async void AppWindow_Destroying(AppWindow sender, object args)
		{
			sender.Destroying -= AppWindow_Destroying;
			propsView = null;

			if (SecurityViewModel is not null)
			{
				// Reload permissions
				await DispatcherQueue.EnqueueAsync(() => SecurityViewModel.GetFilePermissions());
			}
		}
	}
}
