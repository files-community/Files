using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Files.App.Commands;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.ViewModels;
using Files.App.Views;
using Microsoft.UI.Dispatching;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Actions
{
    internal class OpenFolderInNewTabAction : ObservableObject, IAction
	{
		private readonly ICommandContext context = Ioc.Default.GetRequiredService<ICommandContext>();

		public CommandCodes Code => CommandCodes.OpenFolderInNewTab;
		public string Label => "BaseLayoutItemContextFlyoutOpenInNewTab/Text".GetLocalizedResource();

		public IGlyph Glyph { get; } = new Glyph("\uF113") { Family = "CustomGlyph" };

		public bool IsExecutable
		{
			get
			{
				var items = context.ToolbarViewModel?.SelectedItems;
				return items is not null
					&& items.Count < 5
					&& items.All(i => i.PrimaryItemAttribute is StorageItemTypes.Folder);
			}
		}

		public async Task ExecuteAsync()
		{
			var items = context.ToolbarViewModel?.SelectedItems;
			if (items is null)
				return;

			foreach (var item in items)
			{
				await App.Window.DispatcherQueue.EnqueueAsync(async () =>
				{
					string path = item is ShortcutItem shortcut ? shortcut.TargetPath : item.ItemPath;
					await MainPageViewModel.AddNewTabByPathAsync(typeof(PaneHolderPage), path);
				}, DispatcherQueuePriority.Low);
			}
		}
	}
}
