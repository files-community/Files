using Files.Shared;
using Files.App.Dialogs;
using Files.Shared.Enums;
using Files.App.EventArguments;
using Files.App.Filesystem;
using Files.App.Filesystem.FilesystemHistory;
using Files.App.Filesystem.Search;
using Files.App.Helpers;
using Files.Backend.Services.Settings;
using Files.App.UserControls;
using Files.App.UserControls.MultitaskingControl;
using Files.App.ViewModels;
using Files.App.Views.LayoutModes;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Files.App.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using SortDirection = Files.Shared.Enums.SortDirection;
using Files.Backend.Enums;
using Files.Backend.Services;
using Files.Backend.ViewModels.Dialogs.AddItemDialog;
using CommunityToolkit.WinUI;
using Files.App.DataModels;
using Microsoft.UI.Xaml.Controls.Primitives;
using Files.App.Interacts;

namespace Files.App.Views
{
	public sealed partial class BrowserPane : ListBoxItem
	{
		public SolidColorBrush CurrentInstanceBorderBrush
		{
			get { return (SolidColorBrush)GetValue(CurrentInstanceBorderBrushProperty); }
			set { SetValue(CurrentInstanceBorderBrushProperty, value); }
		}

		public Thickness CurrentInstanceBorderThickness
		{
			get { return (Thickness)GetValue(CurrentInstanceBorderThicknessProperty); }
			set { SetValue(CurrentInstanceBorderThicknessProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CurrentInstanceBorderThickness.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty CurrentInstanceBorderThicknessProperty =
			DependencyProperty.Register("CurrentInstanceBorderThickness", typeof(Thickness), typeof(BrowserPane), new PropertyMetadata(null));

		public static readonly DependencyProperty CurrentInstanceBorderBrushProperty =
			DependencyProperty.Register("CurrentInstanceBorderBrush", typeof(SolidColorBrush), typeof(BrowserPane), new PropertyMetadata(null));

		public BrowserPane() : base()
		{
            
		}


		/*
		 * Ensure that the path bar gets updated for user interaction
		 * whenever the path changes. We will get the individual directories from
		 * the updated, most-current path and add them to the UI.
		 */

		private async void ModernShellPage_TextChanged(ISearchBox sender, SearchBoxTextChangedEventArgs e)
		{
			if (e.Reason == SearchBoxTextChangeReason.UserInput)
			{
				if (!string.IsNullOrWhiteSpace(sender.Query))
				{
					var search = new FolderSearch
					{
						Query = sender.Query,
						Folder = FilesystemViewModel.WorkingDirectory,
						MaxItemCount = 10,
						SearchUnindexedItems = UserSettingsService.PreferencesSettingsService.SearchUnindexedItems
					};
					sender.SetSuggestions(await search.SearchAsync());
				}
				else
				{
					sender.ClearSuggestions();
				}
			}
		}

		private async void ModernShellPage_QuerySubmitted(ISearchBox sender, SearchBoxQuerySubmittedEventArgs e)
		{
			if (e.ChosenSuggestion is ListedItem item)
			{
				await NavigationHelpers.OpenPath(item.ItemPath, this);
			}
			else if (e.ChosenSuggestion is null && !string.IsNullOrWhiteSpace(sender.Query))
			{
				SubmitSearch(sender.Query, UserSettingsService.PreferencesSettingsService.SearchUnindexedItems);
			}
		}

		public void SubmitSearch(string query, bool searchUnindexedItems)
		{
			viewModel.FilesystemViewModel.CancelSearch();
			currentInstanceViewModel.CurrentSearchQuery = query;
			InstanceViewModel.SearchedUnindexedItems = searchUnindexedItems;
			ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(FilesystemViewModel.WorkingDirectory), new LayoutModeArguments()
			{
				AssociatedTabInstance = this,
				IsSearchResultPage = true,
				SearchPathParam = FilesystemViewModel.WorkingDirectory,
				SearchQuery = query,
				SearchUnindexedItems = searchUnindexedItems,
			});
		}

		private void ModernShellPage_RefreshRequested(object sender, EventArgs e)
		{
			Refresh_Click();
		}

		private void ModernShellPage_UpNavRequested(object sender, EventArgs e)
		{
			Up_Click();
		}

		private void ModernShellPage_ForwardNavRequested(object sender, EventArgs e)
		{
			Forward_Click();
		}

		private void ModernShellPage_BackNavRequested(object sender, EventArgs e)
		{
			Back_Click();
		}

		protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			base.OnNavigatedTo(eventArgs);
			if (eventArgs.Parameter is string navPath)
			{
				NavParams = new NavigationParams { NavPath = navPath };
			}
			else if (eventArgs.Parameter is NavigationParams navParams)
			{
				NavParams = navParams;
			}
		}

		private void AppSettings_SortDirectionPreferenceUpdated(object sender, SortDirection e)
		{
			FilesystemViewModel?.UpdateSortDirectionStatus();
		}

		private void AppSettings_SortOptionPreferenceUpdated(object sender, SortOption e)
		{
			FilesystemViewModel?.UpdateSortOptionStatus();
		}

		private void AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated(object sender, bool e)
		{
			FilesystemViewModel?.UpdateSortDirectoriesAlongsideFiles();
		}

		private void CoreWindow_PointerPressed(object sender, PointerRoutedEventArgs args)
		{
			if (IsCurrentInstance)
			{
				if (args.GetCurrentPoint(this).Properties.IsXButton1Pressed)
				{
					Back_Click();
				}
				else if (args.GetCurrentPoint(this).Properties.IsXButton2Pressed)
				{
					Forward_Click();
				}
			}
		}

		private async void ModernShellPage_PathBoxItemDropped(object sender, PathBoxItemDroppedEventArgs e)
		{
			await FilesystemHelpers.PerformOperationTypeAsync(e.AcceptedOperation, e.Package, e.Path, false, true);
			e.SignalEvent?.Set();
		}

		private void ModernShellPage_AddressBarTextEntered(object sender, AddressBarTextEnteredEventArgs e)
		{
			ToolbarViewModel.SetAddressBarSuggestions(e.AddressBarTextField, this);
		}

		private async void ModernShellPage_ToolbarPathItemLoaded(object sender, ToolbarPathItemLoadedEventArgs e)
		{
			await ToolbarViewModel.SetPathBoxDropDownFlyoutAsync(e.OpenedFlyout, e.Item, this);
		}

		private async void ModernShellPage_ToolbarFlyoutOpened(object sender, ToolbarFlyoutOpenedEventArgs e)
		{
			await ToolbarViewModel.SetPathBoxDropDownFlyoutAsync(e.OpenedFlyout, (e.OpenedFlyout.Target as FontIcon).DataContext as PathBoxItem, this);
		}

		private void ModernShellPage_NavigationRequested(object sender, PathNavigationEventArgs e)
		{
			ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(e.ItemPath), new LayoutModeArguments()
			{
				NavPathParam = e.ItemPath,
				AssociatedTabInstance = this
			});
		}



		private void ModernShellPage_BackRequested(object sender, BackRequestedEventArgs e)
		{
			if (IsCurrentInstance)
			{
				if (ItemDisplayFrame.CanGoBack)
				{
					e.Handled = true;
					Back_Click();
				}
				else
				{
					e.Handled = false;
				}
			}
		}

		private void DrivesManager_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName is string pn && pn.Equals("ShowUserConsentOnInit"))
			{
				UIHelpers.RequestFilesystemConsentAsync();
			}
		}


		private void FilesystemViewModel_PageTypeUpdated(object sender, PageTypeUpdatedEventArgs e)
		{
			InstanceViewModel.IsPageTypeCloudDrive = e.IsTypeCloudDrive;
		}

		private void FilesystemViewModel_OnSelectionRequestedEvent(object sender, List<ListedItem> e)
		{
			// set focus since selection might occur before the UI finishes updating
			ContentPage.ItemManipulationModel.FocusFileList();
			ContentPage.ItemManipulationModel.SetSelectedItems(e);
		}

		private void FilesystemViewModel_DirectoryInfoUpdated(object sender, EventArgs e)
		{
			if (ContentPage != null)
			{
				if (FilesystemViewModel.FilesAndFolders.Count == 1)
				{
					ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = $"{FilesystemViewModel.FilesAndFolders.Count} {"ItemCount/Text".GetLocalizedResource()}";
				}
				else
				{
					ContentPage.DirectoryPropertiesViewModel.DirectoryItemCount = $"{FilesystemViewModel.FilesAndFolders.Count} {"ItemsCount/Text".GetLocalizedResource()}";
				}
				ContentPage.UpdateSelectionSize();
			}
		}

		private void ViewModel_WorkingDirectoryModified(object sender, WorkingDirectoryModifiedEventArgs e)
		{
			if (!string.IsNullOrWhiteSpace(e.Path))
			{
				if (e.IsLibrary)
				{
					UpdatePathUIToWorkingDirectory(null, e.Name);
				}
				else
				{
					UpdatePathUIToWorkingDirectory(e.Path);
				}
			}
		}

		private async void ItemDisplayFrame_Navigated(object sender, NavigationEventArgs e)
		{
			ToolbarViewModel.SearchBox.Query = string.Empty;
			ToolbarViewModel.IsSearchBoxVisible = false;
			ToolbarViewModel.UpdateAdditionalActions();
			if (ItemDisplayFrame.CurrentSourcePageType == (typeof(DetailsLayoutBrowser))
				|| ItemDisplayFrame.CurrentSourcePageType == typeof(GridViewBrowser))
			{
				// Reset DataGrid Rows that may be in "cut" command mode
				ContentPage.ResetItemOpacity();
			}
			var parameters = e.Parameter as LayoutModeArguments;
			var isTagSearch = parameters.NavPathParam is not null && parameters.NavPathParam.StartsWith("tag:");
			TabItemArguments = new TabItemArguments()
			{
				PageType = typeof(ModernShellPage),
				NavigationArguments = parameters.IsSearchResultPage && !isTagSearch ? parameters.SearchPathParam : parameters.NavPathParam
			};
		}

		private async void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
		{
			args.Handled = true;
			var ctrl = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Control);
			var shift = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Shift);
			var alt = args.KeyboardAccelerator.Modifiers.HasFlag(VirtualKeyModifiers.Menu);

			var tabInstance = true;

			switch (c: ctrl, s: shift, a: alt, t: tabInstance, k: args.KeyboardAccelerator.Key)
			{
				case (true, false, false, true, VirtualKey.E): // ctrl + e, extract
					{
						if (ToolbarViewModel.CanExtract)
						{
							ToolbarViewModel.ExtractCommand.Execute(null);
						}
						break;
					}

				case (true, false, false, true, VirtualKey.Z): // ctrl + z, undo
					if (!InstanceViewModel.IsPageTypeSearchResults)
					{
						await storageHistoryHelpers.TryUndo();
					}
					break;

				case (true, false, false, true, VirtualKey.Y): // ctrl + y, redo
					if (!InstanceViewModel.IsPageTypeSearchResults)
					{
						await storageHistoryHelpers.TryRedo();
					}
					break;

				case (true, true, false, true, VirtualKey.C):
					{
						SlimContentPage?.CommandsViewModel.CopyPathOfSelectedItemCommand.Execute(null);
						break;
					}

				case (false, false, false, _, VirtualKey.F3): //f3
				case (true, false, false, _, VirtualKey.F): // ctrl + f
					if (tabInstance || CurrentPageType == typeof(WidgetsPage))
					{
						ToolbarViewModel.SwitchSearchBoxVisibility();
					}
					break;

				case (true, true, false, true, VirtualKey.N): // ctrl + shift + n, new item
					if (InstanceViewModel.CanCreateFileInPage)
					{
						var addItemDialogViewModel = new AddItemDialogViewModel();
						await DialogService.ShowDialogAsync(addItemDialogViewModel);
						if (addItemDialogViewModel.ResultType.ItemType != AddItemDialogItemType.Cancel)
						{
							UIFilesystemHelpers.CreateFileFromDialogResultType(
								addItemDialogViewModel.ResultType.ItemType,
								addItemDialogViewModel.ResultType.ItemInfo,
								this);
						}
					}
					break;

				case (false, true, false, true, VirtualKey.Delete): // shift + delete, PermanentDelete
					if (ContentPage.IsItemSelected && !ToolbarViewModel.IsEditModeEnabled && !InstanceViewModel.IsPageTypeSearchResults)
					{
						var items = SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
							item.ItemPath,
							item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
						await FilesystemHelpers.DeleteItemsAsync(items, true, true, true);
					}

					break;

				case (true, false, false, true, VirtualKey.C): // ctrl + c, copy
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
					{
						await UIFilesystemHelpers.CopyItem(this);
					}

					break;

				case (true, false, false, true, VirtualKey.V): // ctrl + v, paste
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults && !ToolbarViewModel.SearchHasFocus)
					{
						await UIFilesystemHelpers.PasteItemAsync(FilesystemViewModel.WorkingDirectory, this);
					}

					break;

				case (true, false, false, true, VirtualKey.X): // ctrl + x, cut
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
					{
						UIFilesystemHelpers.CutItem(this);
					}

					break;

				case (true, false, false, true, VirtualKey.A): // ctrl + a, select all
					if (!ToolbarViewModel.IsEditModeEnabled && !ContentPage.IsRenamingItem)
					{
						this.SlimContentPage.ItemManipulationModel.SelectAllItems();
					}

					break;

				case (true, false, false, true, VirtualKey.D): // ctrl + d, delete item
				case (false, false, false, true, VirtualKey.Delete): // delete, delete item
					if (ContentPage.IsItemSelected && !ContentPage.IsRenamingItem && !InstanceViewModel.IsPageTypeSearchResults)
					{
						var items = SlimContentPage.SelectedItems.ToList().Select((item) => StorageHelpers.FromPathAndType(
							item.ItemPath,
							item.PrimaryItemAttribute == StorageItemTypes.File ? FilesystemItemType.File : FilesystemItemType.Directory));
						await FilesystemHelpers.DeleteItemsAsync(items, true, false, true);
					}

					break;

				case (true, false, false, true, VirtualKey.P): // ctrl + p, toggle preview pane
					App.PaneViewModel.IsPreviewSelected = !App.PaneViewModel.IsPreviewSelected;
					break;

				case (true, false, false, true, VirtualKey.R): // ctrl + r, refresh
					if (ToolbarViewModel.CanRefresh)
					{
						Refresh_Click();
					}
					break;

				case (false, false, true, _, VirtualKey.D): // alt + d, select address bar (english)
				case (true, false, false, _, VirtualKey.L): // ctrl + l, select address bar
					if (tabInstance || CurrentPageType == typeof(WidgetsPage))
					{
						ToolbarViewModel.IsEditModeEnabled = true;
					}
					break;

				case (true, true, false, true, VirtualKey.K): // ctrl + shift + k, duplicate tab
					await NavigationHelpers.OpenPathInNewTab(this.FilesystemViewModel.WorkingDirectory);
					break;

				case (true, false, false, true, VirtualKey.H): // ctrl + h, toggle hidden folder visibility
					UserSettingsService.PreferencesSettingsService.AreHiddenItemsVisible ^= true; // flip bool
					break;

				case (false, false, false, _, VirtualKey.F1): // F1, open Files wiki
					await Launcher.LaunchUriAsync(new Uri(@"https://files.community/docs"));
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

			switch (args.KeyboardAccelerator.Key)
			{
				case VirtualKey.F2: //F2, rename
					if (CurrentPageType == typeof(DetailsLayoutBrowser)
						|| CurrentPageType == typeof(GridViewBrowser))
					{
						if (ContentPage.IsItemSelected)
						{
							ContentPage.ItemManipulationModel.StartRenameItem();
						}
					}
					break;
			}
		}

		public async void Refresh_Click()
		{
			if (InstanceViewModel.IsPageTypeSearchResults)
			{
				ToolbarViewModel.CanRefresh = false;
				var searchInstance = new FolderSearch
				{
					Query = InstanceViewModel.CurrentSearchQuery,
					Folder = FilesystemViewModel.WorkingDirectory,
					ThumbnailSize = InstanceViewModel.FolderSettings.GetIconSize(),
					SearchUnindexedItems = InstanceViewModel.SearchedUnindexedItems
				};
				await FilesystemViewModel.SearchAsync(searchInstance);
			}
			else if (CurrentPageType != typeof(WidgetsPage))
			{
				ToolbarViewModel.CanRefresh = false;
				FilesystemViewModel?.RefreshItems(null);
			}
		}

		public void Back_Click()
		{
			ToolbarViewModel.CanGoBack = false;
			if (ItemDisplayFrame.CanGoBack)
			{
				var previousPageContent = ItemDisplayFrame.BackStack[ItemDisplayFrame.BackStack.Count - 1];
				var previousPageNavPath = previousPageContent.Parameter as LayoutModeArguments;
				previousPageNavPath.IsLayoutSwitch = false;
				if (previousPageContent.SourcePageType != typeof(WidgetsPage))
				{
					// Update layout type
					InstanceViewModel.FolderSettings.GetLayoutType(previousPageNavPath.IsSearchResultPage ? previousPageNavPath.SearchPathParam : previousPageNavPath.NavPathParam);
				}
				SelectSidebarItemFromPath(previousPageContent.SourcePageType);

				if (previousPageContent.SourcePageType == typeof(WidgetsPage))
				{
					ItemDisplayFrame.GoBack(new EntranceNavigationTransitionInfo());
				}
				else
				{
					ItemDisplayFrame.GoBack();
				}
			}
		}

		public void Forward_Click()
		{
			ToolbarViewModel.CanGoForward = false;
			if (ItemDisplayFrame.CanGoForward)
			{
				var incomingPageContent = ItemDisplayFrame.ForwardStack[ItemDisplayFrame.ForwardStack.Count - 1];
				var incomingPageNavPath = incomingPageContent.Parameter as LayoutModeArguments;
				incomingPageNavPath.IsLayoutSwitch = false;
				if (incomingPageContent.SourcePageType != typeof(WidgetsPage))
				{
					// Update layout type
					InstanceViewModel.FolderSettings.GetLayoutType(incomingPageNavPath.IsSearchResultPage ? incomingPageNavPath.SearchPathParam : incomingPageNavPath.NavPathParam);
				}
				SelectSidebarItemFromPath(incomingPageContent.SourcePageType);
				ItemDisplayFrame.GoForward();
			}
		}

		public void Up_Click()
		{
			ToolbarViewModel.CanNavigateToParent = false;
			if (string.IsNullOrEmpty(FilesystemViewModel?.WorkingDirectory))
			{
				return;
			}

			bool isPathRooted = string.Equals(FilesystemViewModel.WorkingDirectory, PathNormalization.GetPathRoot(FilesystemViewModel.WorkingDirectory), StringComparison.OrdinalIgnoreCase);

			if (isPathRooted)
			{
				ItemDisplayFrame.Navigate(typeof(WidgetsPage),
										  new LayoutModeArguments()
										  {
											  NavPathParam = "Home".GetLocalizedResource(),
											  AssociatedTabInstance = this
										  },
										  new SuppressNavigationTransitionInfo());
			}
			else
			{
				string parentDirectoryOfPath = FilesystemViewModel.WorkingDirectory.TrimEnd('\\', '/');

				var lastSlashIndex = parentDirectoryOfPath.LastIndexOf("\\", StringComparison.Ordinal);
				if (lastSlashIndex == -1)
				{
					lastSlashIndex = parentDirectoryOfPath.LastIndexOf("/", StringComparison.Ordinal);
				}
				if (lastSlashIndex != -1)
				{
					parentDirectoryOfPath = FilesystemViewModel.WorkingDirectory.Remove(lastSlashIndex);
				}
				if (parentDirectoryOfPath.EndsWith(":"))
				{
					parentDirectoryOfPath += '\\';
				}

				SelectSidebarItemFromPath();
				ItemDisplayFrame.Navigate(InstanceViewModel.FolderSettings.GetLayoutType(parentDirectoryOfPath),
											  new LayoutModeArguments()
											  {
												  NavPathParam = parentDirectoryOfPath,
												  AssociatedTabInstance = this
											  },
											  new SuppressNavigationTransitionInfo());
			}
		}

		private void SelectSidebarItemFromPath(Type incomingSourcePageType = null)
		{
			if (incomingSourcePageType == typeof(WidgetsPage) && incomingSourcePageType != null)
			{
				ToolbarViewModel.PathControlDisplayText = "Home".GetLocalizedResource();
			}
		}

		public void Dispose()
		{
			PreviewKeyDown -= ModernShellPage_PreviewKeyDown;
			this.PointerPressed -= CoreWindow_PointerPressed;
			//SystemNavigationManager.GetForCurrentView().BackRequested -= ModernShellPage_BackRequested; //WINUI3
			App.DrivesManager.PropertyChanged -= DrivesManager_PropertyChanged;

			ToolbarViewModel.ToolbarPathItemInvoked -= ModernShellPage_NavigationRequested;
			ToolbarViewModel.ToolbarFlyoutOpened -= ModernShellPage_ToolbarFlyoutOpened;
			ToolbarViewModel.ToolbarPathItemLoaded -= ModernShellPage_ToolbarPathItemLoaded;
			ToolbarViewModel.AddressBarTextEntered -= ModernShellPage_AddressBarTextEntered;
			ToolbarViewModel.PathBoxItemDropped -= ModernShellPage_PathBoxItemDropped;
			ToolbarViewModel.BackRequested -= ModernShellPage_BackNavRequested;
			ToolbarViewModel.UpRequested -= ModernShellPage_UpNavRequested;
			ToolbarViewModel.RefreshRequested -= ModernShellPage_RefreshRequested;
			ToolbarViewModel.ForwardRequested -= ModernShellPage_ForwardNavRequested;
			ToolbarViewModel.EditModeEnabled -= NavigationToolbar_EditModeEnabled;
			ToolbarViewModel.ItemDraggedOverPathItem -= ModernShellPage_NavigationRequested;
			ToolbarViewModel.PathBoxQuerySubmitted -= NavigationToolbar_QuerySubmitted;
			ToolbarViewModel.RefreshWidgetsRequested -= ModernShellPage_RefreshWidgetsRequested;

			InstanceViewModel.FolderSettings.LayoutPreferencesUpdateRequired -= FolderSettings_LayoutPreferencesUpdateRequired;
			InstanceViewModel.FolderSettings.SortDirectionPreferenceUpdated -= AppSettings_SortDirectionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortOptionPreferenceUpdated -= AppSettings_SortOptionPreferenceUpdated;
			InstanceViewModel.FolderSettings.SortDirectoriesAlongsideFilesPreferenceUpdated -= AppSettings_SortDirectoriesAlongsideFilesPreferenceUpdated;

			if (FilesystemViewModel != null) // Prevent weird case of this being null when many tabs are opened/closed quickly
			{
				FilesystemViewModel.WorkingDirectoryModified -= ViewModel_WorkingDirectoryModified;
				FilesystemViewModel.ItemLoadStatusChanged -= FilesystemViewModel_ItemLoadStatusChanged;
				FilesystemViewModel.DirectoryInfoUpdated -= FilesystemViewModel_DirectoryInfoUpdated;
				FilesystemViewModel.PageTypeUpdated -= FilesystemViewModel_PageTypeUpdated;
				FilesystemViewModel.OnSelectionRequestedEvent -= FilesystemViewModel_OnSelectionRequestedEvent;
				FilesystemViewModel.Dispose();
			}

			if (ItemDisplayFrame.Content is IDisposable disposableContent)
			{
				disposableContent?.Dispose();
			}
		}

		private void FilesystemViewModel_ItemLoadStatusChanged(object sender, ItemLoadStatusChangedEventArgs e)
		{
			switch (e.Status)
			{
				case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Starting:
					ToolbarViewModel.CanRefresh = false;
					SetLoadingIndicatorForTabs(true);
					break;

				case ItemLoadStatusChangedEventArgs.ItemLoadStatus.InProgress:
					SetLoadingIndicatorForTabs(true);
					ToolbarViewModel.CanGoBack = ItemDisplayFrame.CanGoBack;
					ToolbarViewModel.CanGoForward = ItemDisplayFrame.CanGoForward;
					break;

				case ItemLoadStatusChangedEventArgs.ItemLoadStatus.Complete:
					ToolbarViewModel.CanRefresh = true;
					SetLoadingIndicatorForTabs(false);
					// Select previous directory
					if (!InstanceViewModel.IsPageTypeSearchResults && !string.IsNullOrWhiteSpace(e.PreviousDirectory))
					{
						if (e.PreviousDirectory.Contains(e.Path, StringComparison.Ordinal) && !e.PreviousDirectory.Contains("Shell:RecycleBinFolder", StringComparison.Ordinal))
						{
							// Remove the WorkingDir from previous dir
							e.PreviousDirectory = e.PreviousDirectory.Replace(e.Path, string.Empty, StringComparison.Ordinal);

							// Get previous dir name
							if (e.PreviousDirectory.StartsWith('\\'))
							{
								e.PreviousDirectory = e.PreviousDirectory.Remove(0, 1);
							}
							if (e.PreviousDirectory.Contains('\\'))
							{
								e.PreviousDirectory = e.PreviousDirectory.Split('\\')[0];
							}

							// Get the first folder and combine it with WorkingDir
							string folderToSelect = string.Format("{0}\\{1}", e.Path, e.PreviousDirectory);

							// Make sure we don't get double \\ in the e.Path
							folderToSelect = folderToSelect.Replace("\\\\", "\\", StringComparison.Ordinal);

							if (folderToSelect.EndsWith('\\'))
							{
								folderToSelect = folderToSelect.Remove(folderToSelect.Length - 1, 1);
							}

							ListedItem itemToSelect = FilesystemViewModel.FilesAndFolders.Where((item) => item.ItemPath == folderToSelect).FirstOrDefault();

							if (itemToSelect != null && ContentPage != null)
							{
								ContentPage.ItemManipulationModel.SetSelectedItem(itemToSelect);
								ContentPage.ItemManipulationModel.ScrollIntoView(itemToSelect);
							}
						}
					}
					break;
			}
		}

		private void SetLoadingIndicatorForTabs(bool isLoading)
		{
			var multitaskingControls = ((App.Window.Content as Frame).Content as MainPage).ViewModel.MultitaskingControls;

			foreach (var x in multitaskingControls)
			{
				x.SetLoadingIndicatorStatus(x.Items.FirstOrDefault(x => x.Control.TabItemContent == PaneHolder), isLoading);
			}
		}

		public Task TabItemDragOver(object sender, DragEventArgs e) => SlimContentPage?.CommandsViewModel.CommandsModel.DragOver(e);

		public Task TabItemDrop(object sender, DragEventArgs e) => SlimContentPage?.CommandsViewModel.CommandsModel.Drop(e);

		public void NavigateHome()
		{
			ItemDisplayFrame.Navigate(typeof(WidgetsPage),
				new LayoutModeArguments()
				{
					NavPathParam = "Home".GetLocalizedResource(),
					AssociatedTabInstance = this
				},
				new EntranceNavigationTransitionInfo());
		}

		public void NavigateWithArguments(Type sourcePageType, LayoutModeArguments navArgs)
		{
			NavigateToPath(navArgs.NavPathParam, sourcePageType, navArgs);
		}

		public void NavigateToPath(string navigationPath, LayoutModeArguments navArgs = null)
		{
			NavigateToPath(navigationPath, FolderSettings.GetLayoutType(navigationPath), navArgs);
		}

		public void NavigateToPath(string navigationPath, Type sourcePageType, LayoutModeArguments navArgs = null)
		{
			if (sourcePageType == null && !string.IsNullOrEmpty(navigationPath))
			{
				sourcePageType = InstanceViewModel.FolderSettings.GetLayoutType(navigationPath);
			}

			if (navArgs != null && navArgs.AssociatedTabInstance != null)
			{
				ItemDisplayFrame.Navigate(
				sourcePageType,
				navArgs,
				new SuppressNavigationTransitionInfo());
			}
			else
			{
				if (string.IsNullOrEmpty(navigationPath) ||
					string.IsNullOrEmpty(FilesystemViewModel?.WorkingDirectory) ||
					navigationPath.TrimEnd(Path.DirectorySeparatorChar).Equals(
						FilesystemViewModel.WorkingDirectory.TrimEnd(Path.DirectorySeparatorChar),
						StringComparison.OrdinalIgnoreCase)) // return if already selected
				{
					if (InstanceViewModel?.FolderSettings is FolderSettingsViewModel fsModel)
					{
						fsModel.IsLayoutModeChanging = false;
					}
					return;
				}

				NavigationTransitionInfo transition = new SuppressNavigationTransitionInfo();

				if (sourcePageType == typeof(WidgetsPage)
					|| ItemDisplayFrame.Content.GetType() == typeof(WidgetsPage) &&
					(sourcePageType == typeof(DetailsLayoutBrowser) || sourcePageType == typeof(GridViewBrowser)))
				{
					transition = new SuppressNavigationTransitionInfo();
				}

				ItemDisplayFrame.Navigate(
				sourcePageType,
				new LayoutModeArguments()
				{
					NavPathParam = navigationPath,
					AssociatedTabInstance = this
				},
				transition);
			}
		}
	}

	public class PathBoxItem
	{
		public string Title { get; set; }
		public string Path { get; set; }
	}
}
