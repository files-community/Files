// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Files.App.Filesystem;
using Files.App.UserControls.MultitaskingControl;
using Files.App.ViewModels;
using Files.App.Views.LayoutModes;
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

		public Type PageLayoutType => ShellPage?.CurrentPageType ?? typeof(DetailsLayoutBrowser);

		private ContentPageTypes pageType = ContentPageTypes.None;
		public ContentPageTypes PageType => pageType;

		public ListedItem? Folder => ShellPage?.FilesystemViewModel?.CurrentFolder;

		public bool HasItem => ShellPage?.ToolbarViewModel?.HasItem ?? false;

		public bool HasSelection => SelectedItems.Count is not 0;
		public ListedItem? SelectedItem => SelectedItems.Count is 1 ? SelectedItems[0] : null;

		private IReadOnlyList<ListedItem> selectedItems = emptyItems;
		public IReadOnlyList<ListedItem> SelectedItems => selectedItems;

		public bool CanRefresh => ShellPage is not null && ShellPage.ToolbarViewModel.CanRefresh;

		public bool CanGoBack => ShellPage is not null && ShellPage.ToolbarViewModel.CanGoBack;

		public bool CanGoForward => ShellPage is not null && ShellPage.ToolbarViewModel.CanGoForward;

		public bool CanNavigateToParent => ShellPage is not null && ShellPage.ToolbarViewModel.CanNavigateToParent;

		public bool IsSearchBoxVisible => ShellPage is not null && ShellPage.ToolbarViewModel.IsSearchBoxVisible;

		public bool CanCreateItem => GetCanCreateItem();

		public bool IsMultiPaneEnabled => ShellPage is not null && ShellPage.PaneHolder is not null && ShellPage.PaneHolder.IsMultiPaneEnabled;

		public bool IsMultiPaneActive => ShellPage is not null && ShellPage.PaneHolder is not null && ShellPage.PaneHolder.IsMultiPaneActive;

		public bool ShowSearchUnindexedItemsMessage => ShellPage is not null && ShellPage.InstanceViewModel.ShowSearchUnindexedItemsMessage;

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
				page.PropertyChanged -= Page_PropertyChanged;
				page.ContentChanged -= Page_ContentChanged;
				page.InstanceViewModel.PropertyChanged -= InstanceViewModel_PropertyChanged;
				page.ToolbarViewModel.PropertyChanged -= ToolbarViewModel_PropertyChanged;

				if (page.PaneHolder is not null)
					page.PaneHolder.PropertyChanged -= PaneHolder_PropertyChanged;
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
				page.PropertyChanged += Page_PropertyChanged;
				page.ContentChanged += Page_ContentChanged;
				page.InstanceViewModel.PropertyChanged += InstanceViewModel_PropertyChanged;
				page.ToolbarViewModel.PropertyChanged += ToolbarViewModel_PropertyChanged;
				
				if (page.PaneHolder is not null)
					page.PaneHolder.PropertyChanged += PaneHolder_PropertyChanged;
			}

			filesystemViewModel = ShellPage?.FilesystemViewModel;
			if (filesystemViewModel is not null)
				filesystemViewModel.PropertyChanged += FilesystemViewModel_PropertyChanged;

			Update();
			OnPropertyChanged(nameof(ShellPage));
		}

		private void Page_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ShellPage.CurrentPageType):
					OnPropertyChanged(nameof(PageLayoutType));
					break;
				case nameof(ShellPage.PaneHolder):
					OnPropertyChanged(nameof(IsMultiPaneEnabled));
					OnPropertyChanged(nameof(IsMultiPaneActive));
					break;
			}
		}

		private void Page_ContentChanged(object? sender, TabItemArguments e) => Update();

		private void PaneHolder_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(IPaneHolder.IsMultiPaneEnabled):
				case nameof(IPaneHolder.IsMultiPaneActive):
					OnPropertyChanged(e.PropertyName);
					break;
			}
		}

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
				case nameof(CurrentInstanceViewModel.ShowSearchUnindexedItemsMessage):
					OnPropertyChanged(nameof(ShowSearchUnindexedItemsMessage));
					break;
			}
		}

		private void ToolbarViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ToolbarViewModel.CanGoBack):
				case nameof(ToolbarViewModel.CanGoForward):
				case nameof(ToolbarViewModel.CanNavigateToParent):
				case nameof(ToolbarViewModel.HasItem):
				case nameof(ToolbarViewModel.CanRefresh):
				case nameof(ToolbarViewModel.IsSearchBoxVisible):
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
			OnPropertyChanged(nameof(CanGoBack));
			OnPropertyChanged(nameof(CanGoForward));
			OnPropertyChanged(nameof(CanNavigateToParent));
			OnPropertyChanged(nameof(CanRefresh));
			OnPropertyChanged(nameof(CanCreateItem));
			OnPropertyChanged(nameof(IsMultiPaneEnabled));
			OnPropertyChanged(nameof(IsMultiPaneActive));
			OnPropertyChanged(nameof(ShowSearchUnindexedItemsMessage));
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
			OnPropertyChanged(nameof(CanCreateItem));
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

		private bool GetCanCreateItem()
		{
			return ShellPage is not null &&
				pageType is not ContentPageTypes.None
				and not ContentPageTypes.Home
				and not ContentPageTypes.RecycleBin
				and not ContentPageTypes.ZipFolder
				and not ContentPageTypes.SearchResults
				and not ContentPageTypes.MtpDevice;
		}
	}
}
