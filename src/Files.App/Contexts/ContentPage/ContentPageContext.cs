using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Filesystem;
using Files.App.UserControls.MultitaskingControl;
using Files.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;

namespace Files.App.Contexts
{
	internal class ContentPageContext : ObservableObject, IContentPageContext
	{
		private static readonly IReadOnlyList<ListedItem> emptyItems = Enumerable.Empty<ListedItem>().ToImmutableList();

		private readonly IPageContext context = Ioc.Default.GetRequiredService<IPageContext>();

		private ItemViewModel? filesystemViewModel;

		public IShellPage? ShellPage => context?.PaneOrColumn;

		private ContentPageTypes pageType = ContentPageTypes.None;
		public ContentPageTypes PageType => pageType;

		public ListedItem? Folder => ShellPage?.FilesystemViewModel?.CurrentFolder;

		public bool HasItem => ShellPage?.ToolbarViewModel?.HasItem ?? false;

		public bool HasSelection => SelectedItems.Count is not 0;
		public ListedItem? SelectedItem => SelectedItems.Count is 1 ? SelectedItems[0] : null;

		private IReadOnlyList<ListedItem> selectedItems = emptyItems;
		public IReadOnlyList<ListedItem> SelectedItems => selectedItems;

		public ContentPageContext()
		{
			context.Changing += Context_Changing;
			context.Changed += Context_Changed;
			Update();
		}

		private void Context_Changing(object? sender, EventArgs e)
		{
			if (ShellPage is IShellPage page)
			{
				page.ContentChanged -= Page_ContentChanged;
				page.InstanceViewModel.PropertyChanged -= InstanceViewModel_PropertyChanged;
				page.ToolbarViewModel.PropertyChanged -= ToolbarViewModel_PropertyChanged;
			}

			if (filesystemViewModel is not null)
				filesystemViewModel.PropertyChanged -= FilesystemViewModel_PropertyChanged;
			filesystemViewModel = null;

			OnPropertyChanging(nameof(ShellPage));
		}
		private void Context_Changed(object? sender, EventArgs e)
		{
			if (ShellPage is IShellPage page)
			{
				page.ContentChanged += Page_ContentChanged;
				page.InstanceViewModel.PropertyChanged -= InstanceViewModel_PropertyChanged;
				page.ToolbarViewModel.PropertyChanged -= ToolbarViewModel_PropertyChanged;
			}

			filesystemViewModel = ShellPage?.FilesystemViewModel;
			if (filesystemViewModel is not null)
				filesystemViewModel.PropertyChanged += FilesystemViewModel_PropertyChanged;

			Update();
			OnPropertyChanged(nameof(ShellPage));
		}

		private void Page_ContentChanged(object? sender, TabItemArguments e) => Update();

		private void InstanceViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(CurrentInstanceViewModel.IsPageTypeNotHome):
				case nameof(CurrentInstanceViewModel.IsPageTypeRecycleBin):
				case nameof(CurrentInstanceViewModel.IsPageTypeZipFolder):
				case nameof(CurrentInstanceViewModel.IsPageTypeFtp):
				case nameof(CurrentInstanceViewModel.IsPageTypeLibrary):
				case nameof(CurrentInstanceViewModel.IsPageTypeCloudDrive):
				case nameof(CurrentInstanceViewModel.IsPageTypeMtpDevice):
				case nameof(CurrentInstanceViewModel.IsPageTypeSearchResults):
					UpdatePageType();
					break;
			}
		}

		private void ToolbarViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ToolbarViewModel.HasItem):
					OnPropertyChanged(e.PropertyName);
					break;
				case nameof(ToolbarViewModel.SelectedItems):
					UpdateSelectedItems();
					break;
			}
		}

		private void FilesystemViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(ItemViewModel.CurrentFolder))
				OnPropertyChanged(nameof(Folder));
		}

		private void Update()
		{
			UpdatePageType();
			UpdateSelectedItems();

			OnPropertyChanged(nameof(Folder));
			OnPropertyChanged(nameof(HasItem));
		}

		private void UpdatePageType()
		{
			var type = ShellPage?.InstanceViewModel switch
			{
				null => ContentPageTypes.None,
				{ IsPageTypeNotHome: false } => ContentPageTypes.Home,
				{ IsPageTypeRecycleBin: true } => ContentPageTypes.RecycleBin,
				{ IsPageTypeZipFolder: true } => ContentPageTypes.ZipFolder,
				{ IsPageTypeFtp: true } => ContentPageTypes.Ftp,
				{ IsPageTypeLibrary: true } => ContentPageTypes.Library,
				{ IsPageTypeCloudDrive: true } => ContentPageTypes.CloudDrive,
				{ IsPageTypeMtpDevice: true } => ContentPageTypes.MtpDevice,
				{ IsPageTypeSearchResults: true } => ContentPageTypes.SearchResults,
				_ => ContentPageTypes.Folder,
			};
			SetProperty(ref pageType, type, nameof(PageType));
		}

		private void UpdateSelectedItems()
		{
			bool oldHasSelection = HasSelection;
			ListedItem? oldSelectedItem = SelectedItem;

			IReadOnlyList<ListedItem> items = ShellPage?.ToolbarViewModel?.SelectedItems?.AsReadOnly() ?? emptyItems;
			if (SetProperty(ref selectedItems, items, nameof(SelectedItems)))
			{
				if (HasSelection != oldHasSelection)
					OnPropertyChanged(nameof(HasSelection));
				if (SelectedItem != oldSelectedItem)
					OnPropertyChanged(nameof(SelectedItem));
			}
		}
	}
}
