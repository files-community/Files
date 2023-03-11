using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.ViewModels;
using Files.App.Views;
using Files.Backend.Services.Settings;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Actions
{
	internal class OpenInNewTabAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();
		private readonly IUserSettingsService userSettings = Ioc.Default.GetRequiredService<IUserSettingsService>();

		public string Label => "OpenInNewTab".GetLocalizedResource();

		public RichGlyph Glyph = new(opacityStyle: "ColorIconOpenInNewTab");

		public bool IsExecutable => 
			userSettings.PreferencesSettingsService.ShowOpenInNewTab &&
			context.HasSelection &&
			context.SelectedItems.Count < 5 &&
			context.SelectedItems.All(item => item.PrimaryItemAttribute is StorageItemTypes.Folder);

		public OpenInNewTabAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			foreach (var listedItem in context.SelectedItems)
			{
				await App.Window.DispatcherQueue.EnqueueAsync(async () =>
				{
					await MainPageViewModel.AddNewTabByPathAsync(
						typeof(PaneHolderPage),
						(listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath);
				},
				Microsoft.UI.Dispatching.DispatcherQueuePriority.Low);
			}
		}

		private void Context_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
