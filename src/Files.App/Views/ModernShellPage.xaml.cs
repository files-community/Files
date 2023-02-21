using Files.App.EventArguments;
using Files.App.Extensions;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.UserControls;
using Files.App.UserControls.MultitaskingControl;
using Files.App.ViewModels;
using Files.App.Views.LayoutModes;
using Files.Backend.Enums;
using Files.Backend.ViewModels.Dialogs.AddItemDialog;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.System;

namespace Files.App.Views
{
	public sealed partial class ModernShellPage : BaseShellPage
	{
		public override bool CanNavigateBackward => ItemDisplayFrame.CanGoBack;
		public override bool CanNavigateForward => ItemDisplayFrame.CanGoForward;

		protected override Frame ItemDisplay => ItemDisplayFrame;

		public Thickness CurrentInstanceBorderThickness
		{
			get { return (Thickness)GetValue(CurrentInstanceBorderThicknessProperty); }
			set { SetValue(CurrentInstanceBorderThicknessProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CurrentInstanceBorderThickness.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CurrentInstanceBorderThicknessProperty =
			DependencyProperty.Register("CurrentInstanceBorderThickness", typeof(Thickness), typeof(ModernShellPage), new PropertyMetadata(null));

		public ModernShellPage() : base(new CurrentInstanceViewModel())
		{
			InitializeComponent();

			FilesystemViewModel = new ItemViewModel(InstanceViewModel.FolderSettings);
			FilesystemViewModel.WorkingDirectoryModified += ViewModel_WorkingDirectoryModified;
			FilesystemViewModel.ItemLoadStatusChanged += FilesystemViewModel_ItemLoadStatusChanged;
			FilesystemViewModel.DirectoryInfoUpdated += FilesystemViewModel_DirectoryInfoUpdated;
			FilesystemViewModel.PageTypeUpdated += FilesystemViewModel_PageTypeUpdated;
			FilesystemViewModel.OnSelectionRequestedEvent += FilesystemViewModel_OnSelectionRequestedEvent;

			ToolbarViewModel.PathControlDisplayText = "Home".GetLocalizedResource();

			ToolbarViewModel.RefreshWidgetsRequested += ModernShellPage_RefreshWidgetsRequested;
		}

		private void ModernShellPage_RefreshWidgetsRequested(object sender, EventArgs e)
		{
			if (ItemDisplayFrame?.Content is WidgetsPage currentPage)
				currentPage.RefreshWidgetList();
		}

		protected override void FolderSettings_LayoutPreferencesUpdateRequired(object sender, LayoutPreferenceEventArgs e)
		{
			if (FilesystemViewModel is null)
				return;

			FolderSettingsViewModel.SetLayoutPreferencesForPath(FilesystemViewModel.WorkingDirectory, e.LayoutPreference);
			if (e.IsAdaptiveLayoutUpdateRequired)
				AdaptiveLayoutHelpers.ApplyAdaptativeLayout(InstanceViewModel.FolderSettings, FilesystemViewModel.WorkingDirectory, FilesystemViewModel.FilesAndFolders);
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);
			if (eventArgs.Parameter is string navPath)
				NavParams = new NavigationParams { NavPath = navPath };
			else if (eventArgs.Parameter is NavigationParams navParams)
				NavParams = navParams;
		}

		protected override void ShellPage_NavigationRequested(object sender, PathNavigationEventArgs e)
		{
			ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(e.ItemPath), new NavigationArguments()
			{
				NavPathParam = e.ItemPath,
				AssociatedTabInstance = this
			});
		}

		private NavigationParams navParams;
		public NavigationParams NavParams
		{
			get => navParams;
			set
			{
				if (value != navParams)
				{
					navParams = value;
					if (IsLoaded)
						OnNavigationParamsChanged();
				}
			}
		}

		protected override void OnNavigationParamsChanged()
		{
			if (string.IsNullOrEmpty(NavParams?.NavPath) || NavParams.NavPath == "Home")
			{
				ItemDisplayFrame.Navigate(typeof(WidgetsPage),
					new NavigationArguments()
					{
						NavPathParam = NavParams?.NavPath,
						AssociatedTabInstance = this
					}, new SuppressNavigationTransitionInfo());
			}
			else
			{
				var isTagSearch = NavParams.NavPath.StartsWith("tag:");

				ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(NavParams.NavPath),
					new NavigationArguments()
					{
						NavPathParam = NavParams.NavPath,
						SelectItems = !string.IsNullOrWhiteSpace(NavParams?.SelectItem) ? new[] { NavParams.SelectItem } : null,
						IsSearchResultPage = isTagSearch,
						SearchPathParam = isTagSearch ? "Home" : null,
						SearchQuery = isTagSearch ? navParams.NavPath : null,
						AssociatedTabInstance = this
					});
			}
		}

		protected override void ViewModel_WorkingDirectoryModified(object sender, WorkingDirectoryModifiedEventArgs e)
		{
			if (string.IsNullOrWhiteSpace(e.Path))
				return;

			if (e.IsLibrary)
				UpdatePathUIToWorkingDirectory(null, e.Name);
			else
				UpdatePathUIToWorkingDirectory(e.Path);
		}

		private async void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
		{
			ContentPage = await GetContentOrNullAsync();
			if (!ToolbarViewModel.SearchBox.WasQuerySubmitted)
			{
				ToolbarViewModel.SearchBox.Query = string.Empty;
				ToolbarViewModel.IsSearchBoxVisible = false;
			}
			ToolbarViewModel.UpdateAdditionalActions();
			if (ItemDisplayFrame.CurrentSourcePageType == (typeof(DetailsLayoutBrowser))
				|| ItemDisplayFrame.CurrentSourcePageType == typeof(GridViewBrowser))
			{
				// Reset DataGrid Rows that may be in "cut" command mode
				ContentPage.ResetItemOpacity();
			}
			var parameters = e.Parameter as NavigationArguments;
			var isTagSearch = parameters.NavPathParam is not null && parameters.NavPathParam.StartsWith("tag:");
			TabItemArguments = new TabItemArguments()
			{
				InitialPageType = typeof(ModernShellPage),
				NavigationArg = parameters.IsSearchResultPage && !isTagSearch ? parameters.SearchPathParam : parameters.NavPathParam
			};
			if (parameters.IsLayoutSwitch)
				FilesystemViewModel_DirectoryInfoUpdated(sender, EventArgs.Empty);
		}

		private async void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			args.Handled = true;
			var tabInstance = CurrentPageType == typeof(DetailsLayoutBrowser) ||
							  CurrentPageType == typeof(GridViewBrowser);

			// F2, rename
			if (args.KeyboardAccelerator.Key is VirtualKey.F2
				&& tabInstance
				&& ContentPage.IsItemSelected)
			{
				ContentPage.ItemManipulationModel.StartRenameItem();
				return;
			}

			var ctrl = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control);
			var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
			var alt = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);

			switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.KeyboardAccelerator.Key)
			{
				case (true, false, false, true, VirtualKey.E): // ctrl + e, extract
					if (ToolbarViewModel.CanExtract)
						ToolbarViewModel.ExtractCommand.Execute(null);

					break;

				case (true, false, false, true, VirtualKey.Z): // ctrl + z, undo
					if (!InstanceViewModel.IsPageTypeSearchResults)
						await storageHistoryHelpers.TryUndo();

					break;

				case (true, false, false, true, VirtualKey.Y): // ctrl + y, redo
					if (!InstanceViewModel.IsPageTypeSearchResults)
						await storageHistoryHelpers.TryRedo();

					break;

				case (true, true, false, true, VirtualKey.C):
					SlimContentPage?.CommandsViewModel.CopyPathOfSelectedItemCommand.Execute(null);
					break;

				case (false, false, false, _, VirtualKey.F3): //f3
				case (true, false, false, _, VirtualKey.F): // ctrl + f
					if (tabInstance || CurrentPageType == typeof(WidgetsPage))
						ToolbarViewModel.SwitchSearchBoxVisibility();

					break;

				case (true, true, false, true, VirtualKey.N): // ctrl + shift + n, new item
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

				case (false, true, false, true, VirtualKey.Delete): // shift + delete, PermanentDelete
					if (ContentPage.IsItemSelected && !ToolbarViewModel.IsEditModeEnabled && !InstanceViewModel.IsPageTypeSearchResults)
					{
						var items = SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
							item.ItemPath,
							item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
						await FilesystemHelpers.DeleteItemsAsync(items, userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy, true, true);
					}

					break;

				case (true, false, false, true, VirtualKey.C): // ctrl + c, copy
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
						await UIFilesystemHelpers.CopyItem(this);

					break;

				case (true, false, false, true, VirtualKey.V): // ctrl + v, paste
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults && !ToolbarViewModel.SearchHasFocus)
						await UIFilesystemHelpers.PasteItemAsync(FilesystemViewModel.WorkingDirectory, this);

					break;

				case (true, false, false, true, VirtualKey.X): // ctrl + x, cut
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
						UIFilesystemHelpers.CutItem(this);

					break;

				case (true, false, false, true, VirtualKey.A): // ctrl + a, select all
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
						SlimContentPage.ItemManipulationModel.SelectAllItems();

					break;

				case (true, false, false, true, VirtualKey.D): // ctrl + d, delete item
				case (false, false, false, true, VirtualKey.Delete): // delete, delete item
					if (ContentPage.IsItemSelected && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults)
					{
						var items = SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
							item.ItemPath,
							item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
						await FilesystemHelpers.DeleteItemsAsync(items, userSettingsService.FoldersSettingsService.DeleteConfirmationPolicy, false, true);
					}

					break;

				case (true, false, false, true, VirtualKey.P): // ctrl + p, toggle preview pane
					App.PreviewPaneViewModel.IsEnabled = !App.PreviewPaneViewModel.IsEnabled;
					break;

				case (true, false, false, true, VirtualKey.R): // ctrl + r, refresh
					if (ToolbarViewModel.CanRefresh)
						Refresh_Click();

					break;

				case (false, false, true, _, VirtualKey.D): // alt + d, select address bar (english)
				case (true, false, false, _, VirtualKey.L): // ctrl + l, select address bar
					if (tabInstance || CurrentPageType == typeof(WidgetsPage))
						ToolbarViewModel.IsEditModeEnabled = true;

					break;

				case (true, true, false, true, VirtualKey.K): // ctrl + shift + k, duplicate tab
					await NavigationHelpers.OpenPathInNewTab(FilesystemViewModel.WorkingDirectory);
					break;

				case (true, false, false, true, VirtualKey.H): // ctrl + h, toggle hidden folder visibility
					userSettingsService.FoldersSettingsService.ShowHiddenItems ^= true; // flip bool
					break;

				case (true, true, false, _, VirtualKey.Number1): // ctrl+shift+1, details view
					InstanceViewModel.FolderSettings.ToggleLayoutModeDetailsView(true);
					break;

				case (true, true, false, _, VirtualKey.Number2): // ctrl+shift+2, tiles view
					InstanceViewModel.FolderSettings.ToggleLayoutModeTiles(true);
					break;

				case (true, true, false, _, VirtualKey.Number3): // ctrl+shift+3, grid small view
					InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewSmall(true);
					break;

				case (true, true, false, _, VirtualKey.Number4): // ctrl+shift+4, grid medium view
					InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewMedium(true);
					break;

				case (true, true, false, _, VirtualKey.Number5): // ctrl+shift+5, grid large view
					InstanceViewModel.FolderSettings.ToggleLayoutModeGridViewLarge(true);
					break;

				case (true, true, false, _, VirtualKey.Number6): // ctrl+shift+6, column view
					InstanceViewModel.FolderSettings.ToggleLayoutModeColumnView(true);
					break;

				case (true, true, false, _, VirtualKey.Number7): // ctrl+shift+7, adaptive
					InstanceViewModel.FolderSettings.ToggleLayoutModeAdaptive();
					break;
			}
		}

		public override void Back_Click()
		{
			ToolbarViewModel.CanGoBack = false;
			if (!ItemDisplayFrame.CanGoBack)
				return;

			base.Back_Click();
		}

		public override void Forward_Click()
		{
			ToolbarViewModel.CanGoForward = false;
			if (!ItemDisplayFrame.CanGoForward)
				return;

			base.Forward_Click();
		}

		public override void Up_Click()
		{
			ToolbarViewModel.CanNavigateToParent = false;
			if (string.IsNullOrEmpty(FilesystemViewModel?.WorkingDirectory))
				return;

			bool isPathRooted = string.Equals(FilesystemViewModel.WorkingDirectory, PathNormalization.GetPathRoot(FilesystemViewModel.WorkingDirectory), StringComparison.OrdinalIgnoreCase);

			if (isPathRooted)
			{
				ItemDisplayFrame.Navigate(typeof(WidgetsPage),
					new NavigationArguments()
					{
						NavPathParam = "Home",
						AssociatedTabInstance = this
					}, new SuppressNavigationTransitionInfo());
			}
			else
			{
				string parentDirectoryOfPath = FilesystemViewModel.WorkingDirectory.TrimEnd('\\', '/');

				var lastSlashIndex = parentDirectoryOfPath.LastIndexOf("\\", StringComparison.Ordinal);
				if (lastSlashIndex == -1)
					lastSlashIndex = parentDirectoryOfPath.LastIndexOf("/", StringComparison.Ordinal);
				if (lastSlashIndex != -1)
					parentDirectoryOfPath = FilesystemViewModel.WorkingDirectory.Remove(lastSlashIndex);
				if (parentDirectoryOfPath.EndsWith(':'))
					parentDirectoryOfPath += '\\';

				SelectSidebarItemFromPath();
				ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(parentDirectoryOfPath),
					new NavigationArguments()
					{
						NavPathParam = parentDirectoryOfPath,
						AssociatedTabInstance = this
					}, new SuppressNavigationTransitionInfo());
			}
		}

		public override void Dispose()
		{
			ToolbarViewModel.RefreshWidgetsRequested -= ModernShellPage_RefreshWidgetsRequested;

			base.Dispose();
		}

		public override void NavigateHome()
		{
			ItemDisplayFrame.Navigate(typeof(WidgetsPage),
				new NavigationArguments()
				{
					NavPathParam = "Home",
					AssociatedTabInstance = this
				}, new SuppressNavigationTransitionInfo());
		}

		public override void NavigateToPath(string? navigationPath, Type? sourcePageType, NavigationArguments? navArgs = null)
		{
			if (sourcePageType is null && !string.IsNullOrEmpty(navigationPath))
				sourcePageType = InstanceViewModel.FolderSettings.GetLayoutType(navigationPath);

			if (navArgs is not null && navArgs.AssociatedTabInstance is not null)
			{
				ItemDisplayFrame.Navigate(
				sourcePageType,
				navArgs,
				new SuppressNavigationTransitionInfo());
			}
			else
			{
				if ((string.IsNullOrEmpty(navigationPath) ||
					string.IsNullOrEmpty(FilesystemViewModel?.WorkingDirectory) ||
					navigationPath.TrimEnd(Path.DirectorySeparatorChar).Equals(
						FilesystemViewModel.WorkingDirectory.TrimEnd(Path.DirectorySeparatorChar),
						StringComparison.OrdinalIgnoreCase)) &&
					(TabItemArguments.NavigationArg is not string navArg ||
					string.IsNullOrEmpty(navArg) ||
					!navArg.StartsWith("tag:"))) // return if already selected
				{
					if (InstanceViewModel?.FolderSettings is FolderSettingsViewModel fsModel)
						fsModel.IsLayoutModeChanging = false;

					return;
				}

				if (string.IsNullOrEmpty(navigationPath))
					return;

				NavigationTransitionInfo transition = new SuppressNavigationTransitionInfo();

				if (sourcePageType == typeof(WidgetsPage)
					|| ItemDisplayFrame.Content.GetType() == typeof(WidgetsPage) &&
					(sourcePageType == typeof(DetailsLayoutBrowser) || sourcePageType == typeof(GridViewBrowser)))
				{
					transition = new SuppressNavigationTransitionInfo();
				}

				ItemDisplayFrame.Navigate(
				sourcePageType,
				new NavigationArguments()
				{
					NavPathParam = navigationPath,
					AssociatedTabInstance = this
				},
				transition);
			}

			ToolbarViewModel.PathControlDisplayText = FilesystemViewModel.WorkingDirectory;
		}
	}

	public class PathBoxItem
	{
		public string? Title { get; set; }
		public string? Path { get; set; }
	}

	public class NavigationParams
	{
		public string? NavPath { get; set; }
		public string? SelectItem { get; set; }
	}

	public class NavigationArguments
	{
		public bool FocusOnNavigation { get; set; } = false;
		public string? NavPathParam { get; set; } = null;
		public IShellPage? AssociatedTabInstance { get; set; }
		public bool IsSearchResultPage { get; set; } = false;
		public string? SearchPathParam { get; set; } = null;
		public string? SearchQuery { get; set; } = null;
		public bool SearchUnindexedItems { get; set; } = false;
		public bool IsLayoutSwitch { get; set; } = false;
		public IEnumerable<string>? SelectItems { get; set; }
	}
}
