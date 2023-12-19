// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Windows.System;

namespace Files.App.Views.Shells
{
	public sealed partial class ColumnShellPage : BaseShellPage
	{
		public override bool IsCurrentPane
			=> this.FindAscendant<ColumnsLayoutPage>()?.ParentShellPageInstance?.IsCurrentPane ?? false;

		public override bool CanNavigateBackward
			=> false;

		public override bool CanNavigateForward
			=> false;

		protected override Frame ItemDisplay
			=> ItemDisplayFrame;

		private ColumnParam _ColumnParams;
		public ColumnParam ColumnParams
		{
			get => _ColumnParams;
			set
			{
				if (value != _ColumnParams)
				{
					_ColumnParams = value;

					if (IsLoaded)
						OnNavigationParamsChanged();
				}
			}
		}

		public ColumnShellPage() : base(new CurrentInstanceViewModel(FolderLayoutModes.ColumnView))
		{
			InitializeComponent();
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);

			ColumnParams = eventArgs.Parameter as ColumnParam;
			if (ColumnParams?.IsLayoutSwitch ?? false)
				FilesystemViewModel_DirectoryInfoUpdated(this, EventArgs.Empty);
		}

		protected override void ShellPage_NavigationRequested(object sender, PathNavigationEventArgs e)
		{
			this.FindAscendant<ColumnsLayoutPage>()?.SetSelectedPathOrNavigate(e);
		}

		protected override void OnNavigationParamsChanged()
		{
			ItemDisplayFrame.Navigate(
				typeof(ColumnLayoutPage),
				new NavigationArguments()
				{
					IsSearchResultPage = ColumnParams.IsSearchResultPage,
					SearchQuery = ColumnParams.SearchQuery,
					NavPathParam = ColumnParams.NavPathParam,
					SearchUnindexedItems = ColumnParams.SearchUnindexedItems,
					SearchPathParam = ColumnParams.SearchPathParam,
					AssociatedTabInstance = this,
					SelectItems = ColumnParams.SelectItems
				});
		}

		protected override void Page_Loaded(object sender, RoutedEventArgs e)
		{
			FilesystemViewModel = new ItemViewModel(InstanceViewModel?.FolderSettings);
			FilesystemViewModel.WorkingDirectoryModified += ViewModel_WorkingDirectoryModified;
			FilesystemViewModel.ItemLoadStatusChanged += FilesystemViewModel_ItemLoadStatusChanged;
			FilesystemViewModel.DirectoryInfoUpdated += FilesystemViewModel_DirectoryInfoUpdated;
			FilesystemViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;
			FilesystemViewModel.OnSelectionRequestedEvent += FilesystemViewModel_OnSelectionRequestedEvent;
			FilesystemViewModel.GitDirectoryUpdated += FilesystemViewModel_GitDirectoryUpdated;

			PaneHolder = this.FindAscendant<ColumnsLayoutPage>()?.ParentShellPageInstance?.PaneHolder;

			base.Page_Loaded(sender, e);

			NotifyPropertyChanged(nameof(FilesystemViewModel));
		}

		protected override void ViewModel_WorkingDirectoryModified(object sender, WorkingDirectoryModifiedEventArgs e)
		{
			string value = e.Path;
			if (!string.IsNullOrWhiteSpace(value))
				UpdatePathUIToWorkingDirectory(value);
		}

		private async void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
		{
			ContentPage = await GetContentOrNullAsync();

			if (!ToolbarViewModel.SearchBox.WasQuerySubmitted)
			{
				ToolbarViewModel.SearchBox.Query = string.Empty;
				ToolbarViewModel.IsSearchBoxVisible = false;
			}

			if (ItemDisplayFrame.CurrentSourcePageType == typeof(ColumnLayoutPage))
			{
				// Reset DataGrid Rows that may be in "cut" command mode
				ContentPage.ResetItemOpacity();
			}

			var parameters = e.Parameter as NavigationArguments;
			TabItemParameter = new CustomTabViewItemParameter()
			{
				InitialPageType = typeof(ColumnShellPage),
				NavigationParameter = parameters.IsSearchResultPage ? parameters.SearchPathParam : parameters.NavPathParam
			};
		}

		private async void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			args.Handled = true;
			var tabInstance =
				CurrentPageType == typeof(DetailsLayoutPage) ||
				CurrentPageType == typeof(GridLayoutPage) ||
				CurrentPageType == typeof(ColumnsLayoutPage) ||
				CurrentPageType == typeof(ColumnLayoutPage);

			var ctrl = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control);
			var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
			var alt = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);

			switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.KeyboardAccelerator.Key)
			{
				// Ctrl + V, Paste
				case (true, false, false, true, VirtualKey.V):
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults && !ToolbarViewModel.SearchHasFocus)
						await UIFilesystemHelpers.PasteItemAsync(FilesystemViewModel.WorkingDirectory, this);
					break;
			}
		}

		public override void Back_Click()
		{
			ToolbarViewModel.CanGoBack = false;
			if (ItemDisplayFrame.CanGoBack)
				base.Back_Click();
			else
				this.FindAscendant<ColumnsLayoutPage>().NavigateBack();
		}

		public override void Forward_Click()
		{
			ToolbarViewModel.CanGoForward = false;
			if (ItemDisplayFrame.CanGoForward)
				base.Forward_Click();
			else
				this.FindAscendant<ColumnsLayoutPage>().NavigateForward();
		}

		public override void Up_Click()
		{
			if (!ToolbarViewModel.CanNavigateToParent)
				return;

			this.FindAscendant<ColumnsLayoutPage>()?.NavigateUp();
		}

		public override void NavigateToPath(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null)
		{
			this.FindAscendant<ColumnsLayoutPage>().SetSelectedPathOrNavigate(navigationPath, sourcePageType, navArgs);
		}

		public override void NavigateHome()
		{
			this.FindAscendant<ColumnsLayoutPage>()?.ParentShellPageInstance?.NavigateHome();
		}

		public override Task WhenIsCurrent()
			=> Task.WhenAll(_IsCurrentInstanceTCS.Task, this.FindAscendant<ColumnsLayoutPage>()?.ParentShellPageInstance?.WhenIsCurrent() ?? Task.CompletedTask);

		public void RemoveLastPageFromBackStack()
		{
			ItemDisplayFrame.BackStack.Remove(ItemDisplayFrame.BackStack.Last());
		}

		public void SubmitSearch(string query, bool searchUnindexedItems)
		{
			FilesystemViewModel.CancelSearch();
			InstanceViewModel.CurrentSearchQuery = query;
			InstanceViewModel.SearchedUnindexedItems = searchUnindexedItems;
			ItemDisplayFrame.Navigate(typeof(ColumnLayoutPage), new NavigationArguments()
			{
				AssociatedTabInstance = this,
				IsSearchResultPage = true,
				SearchPathParam = FilesystemViewModel.WorkingDirectory,
				SearchQuery = query,
				SearchUnindexedItems = searchUnindexedItems,
			});

			//this.FindAscendant<ColumnViewBrowser>().SetSelectedPathOrNavigate(null, typeof(ColumnViewBase), navArgs);
		}
	}
}
