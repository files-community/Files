using CommunityToolkit.WinUI.UI;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.UserControls;
using Files.App.UserControls.MultitaskingControl;
using Files.App.ViewModels;
using Files.App.Views.LayoutModes;
using Files.Backend.Enums;
using Files.Backend.ViewModels.Dialogs.AddItemDialog;
using Files.Shared.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;
using Windows.Storage;
using Windows.System;

namespace Files.App.Views
{
	public sealed partial class ColumnShellPage : BaseShellPage
	{
		public override bool CanNavigateBackward => false;

		public override bool CanNavigateForward => false;

		protected override Frame ItemDisplay => ItemDisplayFrame;

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
			=> this.FindAscendant<ColumnViewBrowser>().SetSelectedPathOrNavigate(e);

		private ColumnParam columnParams;
		public ColumnParam ColumnParams
		{
			get => columnParams;
			set
			{
				if (value != columnParams)
				{
					columnParams = value;
					if (IsLoaded)
						OnNavigationParamsChanged();
				}
			}
		}

		protected override void OnNavigationParamsChanged()
		{
			ItemDisplayFrame.Navigate(
				typeof(ColumnViewBase),
				new NavigationArguments()
				{
					IsSearchResultPage = columnParams.IsSearchResultPage,
					SearchQuery = columnParams.SearchQuery,
					NavPathParam = columnParams.NavPathParam,
					SearchUnindexedItems = columnParams.SearchUnindexedItems,
					SearchPathParam = columnParams.SearchPathParam,
					AssociatedTabInstance = this
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

			if (ItemDisplayFrame.CurrentSourcePageType == typeof(ColumnViewBase))
			{
				// Reset DataGrid Rows that may be in "cut" command mode
				ContentPage.ResetItemOpacity();
			}

			var parameters = e.Parameter as NavigationArguments;
			TabItemArguments = new TabItemArguments()
			{
				InitialPageType = typeof(ColumnShellPage),
				NavigationArg = parameters.IsSearchResultPage ? parameters.SearchPathParam : parameters.NavPathParam
			};
		}

		private async void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			args.Handled = true;
			var tabInstance =
				CurrentPageType == typeof(DetailsLayoutBrowser) ||
				CurrentPageType == typeof(GridViewBrowser) ||
				CurrentPageType == typeof(ColumnViewBrowser) ||
				CurrentPageType == typeof(ColumnViewBase);

			var ctrl = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control);
			var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
			var alt = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);

			switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.KeyboardAccelerator.Key)
			{
				// Ctrl + Shift + N: New item
				case (true, true, false, true, VirtualKey.N):
					if (InstanceViewModel.CanCreateFileInPage)
					{
						var addItemDialogViewModel = new AddItemDialogViewModel();
						await dialogService.ShowDialogAsync(addItemDialogViewModel);
						if (addItemDialogViewModel.ResultType.ItemType == AddItemDialogItemType.Shortcut)
							CreateNewShortcutFromDialog();
						else if (addItemDialogViewModel.ResultType.ItemType != AddItemDialogItemType.Cancel)
							UIFilesystemHelpers.CreateFileFromDialogResultType(
								addItemDialogViewModel.ResultType.ItemType,
								addItemDialogViewModel.ResultType.ItemInfo,
								this);
					}
					break;
				// Ctrl + V, Paste
				case (true, false, false, true, VirtualKey.V):
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults && !ToolbarViewModel.SearchHasFocus)
						await UIFilesystemHelpers.PasteItemAsync(FilesystemViewModel.WorkingDirectory, this);
					break;
				// Alt + D, Select address bar (English)
				case (false, false, true, true, VirtualKey.D):
				// Ctrl + L, Select address bar
				case (true, false, false, true, VirtualKey.L):
					ToolbarViewModel.IsEditModeEnabled = true;
					break;
			}
		}

		public override void Back_Click()
		{
			ToolbarViewModel.CanGoBack = false;
			if (ItemDisplayFrame.CanGoBack)
				base.Back_Click();
			else
				this.FindAscendant<ColumnViewBrowser>().NavigateBack();
		}

		public override void Forward_Click()
		{
			ToolbarViewModel.CanGoForward = false;
			if (ItemDisplayFrame.CanGoForward)
				base.Forward_Click();
			else
				this.FindAscendant<ColumnViewBrowser>().NavigateForward();
		}

		public override void Up_Click()
		{
			this.FindAscendant<ColumnViewBrowser>()?.NavigateUp();
		}

		public override void NavigateToPath(string navigationPath, Type sourcePageType, NavigationArguments navArgs = null)
		{
			this.FindAscendant<ColumnViewBrowser>().SetSelectedPathOrNavigate(navigationPath, sourcePageType, navArgs);
		}

		public override void NavigateHome()
			=> throw new NotImplementedException("Can't show HomePage in ColumnView");

		public void RemoveLastPageFromBackStack()
		{
			ItemDisplayFrame.BackStack.Remove(ItemDisplayFrame.BackStack.Last());
		}

		public void SubmitSearch(string query, bool searchUnindexedItems)
		{
			FilesystemViewModel.CancelSearch();
			InstanceViewModel.CurrentSearchQuery = query;
			InstanceViewModel.SearchedUnindexedItems = searchUnindexedItems;
			ItemDisplayFrame.Navigate(typeof(ColumnViewBase), new NavigationArguments()
			{
				AssociatedTabInstance = this,
				IsSearchResultPage = true,
				SearchPathParam = FilesystemViewModel.WorkingDirectory,
				SearchQuery = query,
				SearchUnindexedItems = searchUnindexedItems,
			});

			//this.FindAscendant<ColumnViewBrowser>().SetSelectedPathOrNavigate(null, typeof(ColumnViewBase), navArgs);
		}

		private async void CreateNewShortcutFromDialog()
			=> await UIFilesystemHelpers.CreateShortcutFromDialogAsync(this);
	}
}
