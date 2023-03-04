using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.App.ViewModels.Widgets;
using Files.App.ViewModels.Widgets.Bundles;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace Files.App.UserControls.Widgets
{
	public sealed partial class BundlesWidget : UserControl, IWidgetItem, IDisposable
	{
		#region Fields and Properties
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public BundlesViewModel ViewModel { get; set; } = new();

		public string WidgetName
			=> nameof(BundlesWidget);

		public string AutomationProperties
			=> "BundlesWidgetAutomationProperties/Name".GetLocalizedResource();

		public string WidgetHeader
			=> "Bundles".GetLocalizedResource();

		public bool IsWidgetSettingEnabled
			=> UserSettingsService.PreferencesSettingsService.ShowBundlesWidget;
		
		public bool ShowMenuFlyout
			=> false;

		public MenuFlyoutItem? MenuFlyoutItem
			=> null;
		#endregion

		public BundlesWidget()
			=> InitializeComponent();

		public Task RefreshWidget()
			=>Task.CompletedTask;

		public void Dispose()
		{
			// We need dispose to unhook events to avoid memory leaks
			ViewModel?.Dispose();
		}
	}
}
