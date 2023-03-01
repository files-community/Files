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

			OpenSecurityAdvancedPageWindowCommand = new RelayCommand(() => OpenSecurityAdvancedPageWindow());
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
			if (SecurityViewModel is null)
				return;

			if (_isWinUI3)
			{
				if (propsView is null)
				{
					var frame = new Frame()
					{
						RequestedTheme = ThemeHelper.RootTheme
					};

					frame.Navigate(
						typeof(SecurityAdvancedPage),
						new PropertiesPageNavigationArguments() { Item = SecurityViewModel.Item },
						new SuppressNavigationTransitionInfo());

					// Initialize window
					var newWindow = new WinUIEx.WindowEx()
					{
						IsMinimizable = false,
						IsMaximizable = false,
						Content = frame,

						// Set min width/height
						MinWidth = 850,
						MinHeight = 550,

						// Set backdrop
						Backdrop = new WinUIEx.MicaSystemBackdrop(),
					};

					var appWindow = newWindow.AppWindow;

					// Set icon
					appWindow.SetIcon(FilePropertiesHelpers.LogoPath);

					if (frame.Content is SecurityAdvancedPage properties)
					{
						properties.window = newWindow;
						properties.appWindow = appWindow;
					}

					// Customize titlebar
					appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
					appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
					appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
					appWindow.Title = string.Format("SecurityAdvancedPermissionsTitle".GetLocalizedResource(), SecurityViewModel.Item.Name);

					appWindow.Resize(new SizeInt32(850, 550));
					appWindow.Destroying += SecurityAdvancedPageWindow_Destroying;
					appWindow.Show();

					propsView = appWindow;
				}
				else
				{
					propsView.Show(true);
				}
			}
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
