using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.ViewModels;
using Files.App.Views;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Files.App.Actions
{
	internal class OpenInNewTabAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "OpenInNewTab".GetLocalizedResource();

		public RichGlyph Glyph = new(opacityStyle: "ColorIconOpenInNewTab");

		public bool IsExecutable =>
			context.PageType is not ContentPageTypes.RecycleBin &&
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

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IContentPageContext.SelectedItems):
				case nameof(IContentPageContext.PageType):
					OnPropertyChanged(nameof(IsExecutable));
					break;
			}
		}
	}
}
