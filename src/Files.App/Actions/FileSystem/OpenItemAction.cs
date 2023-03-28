using Microsoft.UI.Xaml.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Commands;
using Files.App.Contexts;
using Files.App.Extensions;
using Files.App.Helpers;
using Files.App.Views;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Files.App.Actions
{
	internal class OpenItemAction : XamlUICommand
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "Open".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph => new(opacityStyle: "ColorIconOpenFile");

		public HotKey HotKey => new(VirtualKey.Enter);

		private const int MaxOpenCount = 10;

		public bool CanExecute => context.HasSelection && context.SelectedItems.Count <= MaxOpenCount &&
			!(context.ShellPage is ColumnShellPage && context.SelectedItem?.PrimaryItemAttribute == StorageItemTypes.Folder);

		public OpenItemAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			NavigationHelpers.OpenSelectedItems(context.ShellPage);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				NotifyCanExecuteChanged();
		}
	}

	internal class OpenItemWithApplicationPickerAction : XamlUICommand
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "BaseLayoutItemContextFlyoutOpenItemWith/Text".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph => new(opacityStyle: "ColorIconOpenWith");

		public bool CanExecute => context.HasSelection && context.SelectedItems.All(
				i => (i.PrimaryItemAttribute == StorageItemTypes.File && !i.IsShortcut && !i.IsExecutable) || (i.PrimaryItemAttribute == StorageItemTypes.Folder && i.IsArchive));

		public OpenItemWithApplicationPickerAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			NavigationHelpers.OpenSelectedItems(context.ShellPage, true);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				NotifyCanExecuteChanged();
		}
	}

	internal class OpenParentFolderAction : XamlUICommand
	{
		private readonly IContentPageContext context = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label => "BaseLayoutItemContextFlyoutOpenParentFolder/Text".GetLocalizedResource();

		public string Description => "TODO: Need to be described.";

		public RichGlyph Glyph => new(baseGlyph: "\uE197");

		public bool CanExecute => context.HasSelection && context.ShellPage.InstanceViewModel.IsPageTypeSearchResults;

		public OpenParentFolderAction()
		{
			context.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			var item = context.SelectedItem;
			var folderPath = Path.GetDirectoryName(item.ItemPath.TrimEnd('\\'));

			context.ShellPage.NavigateWithArguments(context.ShellPage.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new NavigationArguments()
			{
				NavPathParam = folderPath,
				SelectItems = new[] { item.ItemNameRaw },
				AssociatedTabInstance = context.ShellPage
			});
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				NotifyCanExecuteChanged();
		}
	}
}
