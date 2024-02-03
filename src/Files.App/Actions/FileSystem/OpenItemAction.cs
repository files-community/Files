// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.IO;
using Windows.Storage;

namespace Files.App.Actions
{
	internal class OpenItemAction : ObservableObject, IAction
	{
		private IContentPageContext ContentPageContext { get; } = Ioc.Default.GetRequiredService<IContentPageContext>();

		public string Label
			=> "Open".GetLocalizedResource();

		public string Description
			=> "OpenItemDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconOpenFile");

		public HotKey HotKey
			=> new(Keys.Enter);

		private const int MaxOpenCount = 10;

		public bool IsExecutable =>
			ContentPageContext.HasSelection &&
			ContentPageContext.SelectedItems.Count <= MaxOpenCount &&
			!(ContentPageContext.ShellPage is ColumnShellPage &&
			ContentPageContext.SelectedItem?.PrimaryItemAttribute == StorageItemTypes.Folder);

		public OpenItemAction()
		{
			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (ContentPageContext.ShellPage is not null)
				return NavigationHelpers.OpenSelectedItemsAsync(ContentPageContext.ShellPage);

			return Task.CompletedTask;
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}

	internal class OpenItemWithApplicationPickerAction : ObservableObject, IAction
	{
		private readonly IContentPageContext ContentPageContext;

		public string Label
			=> "OpenWith".GetLocalizedResource();

		public string Description
			=> "OpenItemWithApplicationPickerDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(opacityStyle: "ColorIconOpenWith");

		public bool IsExecutable =>
			ContentPageContext.HasSelection &&
			ContentPageContext.SelectedItems.All(i =>
				(i.PrimaryItemAttribute == StorageItemTypes.File && !i.IsShortcut && !i.IsExecutable) ||
				(i.PrimaryItemAttribute == StorageItemTypes.Folder && i.IsArchive));

		public OpenItemWithApplicationPickerAction()
		{
			ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public Task ExecuteAsync()
		{
			if (ContentPageContext.ShellPage is null)
				return Task.CompletedTask;

			return NavigationHelpers.OpenSelectedItemsAsync(ContentPageContext.ShellPage, true);
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}

	internal class OpenParentFolderAction : ObservableObject, IAction
	{
		private readonly IContentPageContext ContentPageContext;

		public string Label
			=> "BaseLayoutItemContextFlyoutOpenParentFolder/Text".GetLocalizedResource();

		public string Description
			=> "OpenParentFolderDescription".GetLocalizedResource();

		public RichGlyph Glyph
			=> new(baseGlyph: "\uE197");

		public bool IsExecutable =>
			ContentPageContext.HasSelection &&
			ContentPageContext.ShellPage is not null &&
			ContentPageContext.ShellPage.InstanceViewModel.IsPageTypeSearchResults;

		public OpenParentFolderAction()
		{
			ContentPageContext = Ioc.Default.GetRequiredService<IContentPageContext>();

			ContentPageContext.PropertyChanged += Context_PropertyChanged;
		}

		public async Task ExecuteAsync()
		{
			if (ContentPageContext.ShellPage is null)
				return;

			var item = ContentPageContext.SelectedItem;
			var folderPath = Path.GetDirectoryName(item?.ItemPath.TrimEnd('\\'));

			if (folderPath is null || item is null)
				return;

			ContentPageContext.ShellPage.NavigateWithArguments(ContentPageContext.ShellPage.InstanceViewModel.FolderSettings.GetLayoutType(folderPath), new NavigationArguments()
			{
				NavPathParam = folderPath,
				SelectItems = new[] { item.ItemNameRaw },
				AssociatedTabInstance = ContentPageContext.ShellPage
			});
		}

		private void Context_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IContentPageContext.HasSelection))
				OnPropertyChanged(nameof(IsExecutable));
		}
	}
}
