using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Filesystem;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Files.App.Actions
{
	internal class OpenInNewWindowAction : ObservableObject, IAction
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "OpenInNewWindow".GetLocalizedResource();

		public RichGlyph Glyph = new(opacityStyle: "ColorIconOpenInNewWindow");

		public bool IsExecutable =>
			context.PageType is not ContentPageTypes.RecycleBin &&
			context.HasSelection &&
			context.SelectedItems.Count < 5 &&
			context.SelectedItems.All(item => item.PrimaryItemAttribute is StorageItemTypes.Folder);

		public OpenInNewWindowAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			foreach (var listedItem in context.SelectedItems)
			{
				var selectedItemPath = (listedItem as ShortcutItem)?.TargetPath ?? listedItem.ItemPath;
				var folderUri = new Uri($"files-uwp:?folder={@selectedItemPath}");
				await Launcher.LaunchUriAsync(folderUri);
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
