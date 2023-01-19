using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Extensions;
using Files.App.ViewModels.Widgets;
using Files.App.ViewModels.Widgets.Bundles;
using Files.Backend.Services.Settings;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.Widgets
{
	public sealed partial class BundlesWidget : UserControl, IWidgetItemModel, IDisposable
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public BundlesViewModel ViewModel
		{
			get => (BundlesViewModel)DataContext;
			private set => DataContext = value;
		}

		public string WidgetName => nameof(BundlesWidget);

		public string AutomationProperties => "BundlesWidgetAutomationProperties/Name".GetLocalizedResource();

		public string WidgetHeader => "Bundles".GetLocalizedResource();

		public bool IsWidgetSettingEnabled => UserSettingsService.PreferencesSettingsService.ShowBundlesWidget;

		public BundlesWidget()
		{
			InitializeComponent();

			ViewModel = new BundlesViewModel();
		}

		public Task RefreshWidget()
		{
			return Task.CompletedTask;
		}

		#region IDisposable

		public void Dispose()
		{
			// We need dispose to unhook events to avoid memory leaks
			ViewModel?.Dispose();

			ViewModel = null;
		}

		#endregion IDisposable
	}
}