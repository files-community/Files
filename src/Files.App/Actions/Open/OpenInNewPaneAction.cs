using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.Backend.Services.Settings;
using System.Threading.Tasks;

namespace Files.App.Actions
{
	internal class OpenInNewPaneAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IUserSettingsService userSettings = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public string Label => "OpenInNewPane".GetLocalizedResource();

		public RichGlyph Glyph = new(opacityStyle: "ColorIconRightPane");

		public bool IsExecutable =>
			userSettings.PreferencesSettingsService.ShowOpenInNewPane &&
			context.PageType is not ContentPageTypes.RecycleBin &&
			context.ShellPage is not null &&
			context.ShellPage.SlimContentPage is not null &&
			context.HasSelection &&
			context.SelectedItems.Count == 1 &&
			context.SelectedItem?.PrimaryItemAttribute is Windows.Storage.StorageItemTypes.Folder;

		public OpenInNewPaneAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			NavigationHelpers.OpenInSecondaryPane(context.ShellPage!, context.SelectedItem!);
		}

		private void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
