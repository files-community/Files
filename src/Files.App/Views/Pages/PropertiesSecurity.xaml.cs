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
using static Files.App.Views.PropertiesSecurityAdvanced;

namespace Files.App.Views
{
	public sealed partial class PropertiesSecurity : PropertiesTab
	{
		public RelayCommand OpenAdvancedPropertiesCommand { get; set; }

		public SecurityProperties SecurityProperties { get; set; }

		private AppWindow? propsView;

		public PropertiesSecurity()
		{
			this.InitializeComponent();

			OpenAdvancedPropertiesCommand = new RelayCommand(() => OpenAdvancedProperties());
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			var np = e.Parameter as Views.Properties.PropertyNavParam;

			if (np.navParameter is ListedItem listedItem)
			{
				SecurityProperties = new SecurityProperties(listedItem);
			}
			else if (np.navParameter is DriveItem driveitem)
			{
				SecurityProperties = new SecurityProperties(driveitem);
			}

			base.OnNavigatedTo(e);
		}

		public async override Task<bool> SaveChangesAsync()
		{
			return SecurityProperties is null || SecurityProperties.SetFilePermissions();
		}

		protected override void Properties_Loaded(object sender, RoutedEventArgs e)
		{
			base.Properties_Loaded(sender, e);

			if (SecurityProperties is not null)
			{
				SecurityProperties.GetFilePermissions();
			}
		}

		public override void Dispose()
		{

		}

		private void OpenAdvancedProperties()
		{
			if (SecurityProperties is null)
			{
				return;
			}

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
			{
				if (propsView is null)
				{
					var frame = new Frame();
					frame.RequestedTheme = ThemeHelper.RootTheme;
					frame.Navigate(typeof(PropertiesSecurityAdvanced), new PropertiesPageNavigationArguments()
					{
						Item = SecurityProperties.Item
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
					if (frame.Content is PropertiesSecurityAdvanced properties)
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

					appWindow.Title = string.Format("SecurityAdvancedPermissionsTitle".GetLocalizedResource(), SecurityProperties.Item.Name);
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

			if (SecurityProperties is not null)
			{
				await DispatcherQueue.EnqueueAsync(() => SecurityProperties.GetFilePermissions()); // Reload permissions
			}
		}
	}
}
