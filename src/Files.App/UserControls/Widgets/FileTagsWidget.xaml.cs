using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.ViewModels.Widgets;
using Files.Backend.Services.Settings;
using Files.Backend.ViewModels.Widgets.FileTagsWidget;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Files.App.UserControls.Widgets
{
	public sealed partial class FileTagsWidget : UserControl, IWidgetItemModel
	{
		private IUserSettingsService UserSettingsService { get; } = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public FileTagsWidgetViewModel ViewModel
		{
			get => (FileTagsWidgetViewModel)DataContext;
			set => DataContext = value;
		}

		public string WidgetName => nameof(BundlesWidget);

		public string WidgetHeader => "File Tags";

		public string AutomationProperties => "File Tags Widget";

		public bool IsWidgetSettingEnabled => UserSettingsService.AppearanceSettingsService.ShowFileTagsWidget;

		public FileTagsWidget()
		{
			InitializeComponent();
			ViewModel = new();
		}

		private void FileTagItem_ItemClick(object sender, ItemClickEventArgs e)
		{
			throw new NotImplementedException();
		}

		public Task RefreshWidget()
		{
			return Task.CompletedTask;
		}

		public void Dispose()
		{
		}
	}
}
