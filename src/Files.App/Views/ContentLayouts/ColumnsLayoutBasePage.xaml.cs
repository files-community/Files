// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.Views.ContentLayouts
{
	/// <summary>
	/// Represents the base page of Column View
	/// </summary>
	public sealed partial class ColumnsLayoutBasePage : BaseLayout
	{
		private readonly IUserSettingsService UserSettingsService = Ioc.Default.GetRequiredService<IUserSettingsService>();

		private readonly ColumnsLayoutBaseViewModel ViewModel;

		public ColumnsLayoutBasePage() : base()
		{
			// Dependency injection
			ViewModel = Ioc.Default.GetRequiredService<ColumnsLayoutBaseViewModel>();
			BaseViewModel = ViewModel;

			var selectionRectangle = RectangleSelection.Create(MainUngroupedListView, SelectionRectangle, MainUngroupedListView_SelectionChanged);
			selectionRectangle.SelectionEnded += ViewModel.SelectionRectangle_SelectionEnded;
		}

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			ViewModel.OnNavigatedTo(e);
		}

		protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			ViewModel.OnNavigatingFrom(e);
		}

		private async void ItemNameTextBox_BeforeTextChanging(TextBox textBox, TextBoxBeforeTextChangingEventArgs args)
		{
			if (ViewModel.IsRenamingItem)
			{
				await ViewModel.ValidateItemNameInputText(textBox, args, (showError) =>
				{
					FileNameTeachingTip.Visibility = showError ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
					FileNameTeachingTip.IsOpen = showError;
				});
			}
		}

		private void MainUngroupedListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ViewModel.SelectedItems = MainUngroupedListView.SelectedItems.Cast<ListedItem>().Where(x => x is not null).ToList();

			if (e is null)
				return;

			if (e.AddedItems.Count > 0)
				ViewModel.ParentColumnsLayoutPage?.HandleSelectionChange(this);

			if (e.RemovedItems.Count > 0 && openedFolderPresenter != null)
			{
				var presenter = openedFolderPresenter.FindDescendant<Grid>()!;
				presenter!.Background = this.Resources["ListViewItemBackgroundSelected"] as SolidColorBrush;
			}

			if (ViewModel.SelectedItems?.Count == 1 &&
				ViewModel.SelectedItem?.PrimaryItemAttribute is StorageItemTypes.Folder &&
				openedFolderPresenter != MainUngroupedListView.ContainerFromItem(ViewModel.SelectedItems))
			{
				if (UserSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick)
				{
					ViewModel.ItemInvoked?.Invoke(new ColumnParam { Source = this, NavPathParam = (ViewModel.SelectedItems is ShortcutItem sht ? sht.TargetPath : SelectedItem.ItemPath), ListView = FileList }, EventArgs.Empty);
				}
				else
				{
					CloseFolder();
				}
			}
			else if (ViewModel.SelectedItems?.Count > 1 ||
				ViewModel.SelectedItem?.PrimaryItemAttribute is StorageItemTypes.File ||
				openedFolderPresenter != null &&
				ViewModel.ParentShellPageInstance != null &&
				!ViewModel.ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.Contains(MainUngroupedListView.ItemFromContainer(openedFolderPresenter)))
			{
				CloseFolder();
			}
		}

		private void MainUngroupedListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
		{
			if (!ViewModel.IsRenamingItem)
				ViewModel.HandleRightClick(sender, e);
		}

		private void MainUngroupedListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			ViewModel.doubleClickTimer.Stop();

			var clickedItem = e.OriginalSource as FrameworkElement;

			if (clickedItem?.DataContext is ListedItem item)
			{
				switch (item.PrimaryItemAttribute)
				{
					case StorageItemTypes.File:
						if (!UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick)
							_ = NavigationHelpers.OpenSelectedItems(ViewModel.ParentShellPageInstance, false);
						break;
					case StorageItemTypes.Folder:
						if (!UserSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick)
							ViewModel.ItemInvoked?.Invoke(new ColumnParam { Source = this, NavPathParam = (item is ShortcutItem sht ? sht.TargetPath : item.ItemPath), ListView = FileList }, EventArgs.Empty);
						break;
					default:
						if (UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
							ViewModel.ParentShellPageInstance.Up_Click();
						break;
				}
			}
			else if (UserSettingsService.FoldersSettingsService.DoubleClickToGoUp)
			{
				ViewModel.ParentShellPageInstance.Up_Click();
			}

			ViewModel.ResetRenameDoubleClick();
		}

		private void MainUngroupedListView_Holding(object sender, HoldingRoutedEventArgs e)
		{
			ViewModel.HandleRightClick(sender, e);
		}

		private async void MainUngroupedListView_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (ViewModel.ParentShellPageInstance is null ||
				ViewModel.IsRenamingItem ||
				ViewModel.SelectedItems?.Count > 1)
				return;

			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);

			if (ctrlPressed && e.Key is VirtualKey.A)
			{
				e.Handled = true;

				var commands = Ioc.Default.GetRequiredService<ICommandManager>();
				var hotKey = new HotKey(Keys.A, KeyModifiers.Ctrl);

				await commands[hotKey].ExecuteAsync();
			}
			else if (e.Key == VirtualKey.Enter && !e.KeyStatus.IsMenuKeyDown)
			{
				e.Handled = true;

				if (ViewModel.IsItemSelected && ViewModel.SelectedItem.PrimaryItemAttribute == StorageItemTypes.Folder)
					ViewModel.ItemInvoked?.Invoke(new ColumnParam { Source = this, NavPathParam = (ViewModel.SelectedItem is ShortcutItem sht ? sht.TargetPath : SelectedItem.ItemPath), ListView = FileList }, EventArgs.Empty);
			}
			else if (e.Key == VirtualKey.Enter && e.KeyStatus.IsMenuKeyDown)
			{
				FilePropertiesHelpers.OpenPropertiesWindow(ViewModel.ParentShellPageInstance);
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Space)
			{
				if (!ViewModel.ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
					e.Handled = true;
			}
			else if (e.KeyStatus.IsMenuKeyDown && (e.Key == VirtualKey.Left || e.Key == VirtualKey.Right || e.Key == VirtualKey.Up))
			{
				// Unfocus the GridView so keyboard shortcut can be handled
				Focus(FocusState.Pointer);
			}
			else if (e.KeyStatus.IsMenuKeyDown && shiftPressed && e.Key == VirtualKey.Add)
			{
				// Unfocus the ListView so keyboard shortcut can be handled (alt + shift + "+")
				Focus(FocusState.Pointer);
			}
			else if (e.Key == VirtualKey.Up || e.Key == VirtualKey.Down)
			{
				ViewModel.ClearOpenedFolderSelectionIndicator();

				// If list has only one item, select it on arrow down/up (#5681)
				if (!ViewModel.IsItemSelected)
				{
					FileList.SelectedIndex = 0;
					e.Handled = true;
				}
			}
			else if (e.Key == VirtualKey.Left) // Left arrow: select parent folder (previous column)
			{
				if (ViewModel.ParentShellPageInstance is not null && ViewModel.ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
					return;

				var currentBladeIndex = (ViewModel.ParentShellPageInstance is ColumnShellPage associatedColumnShellPage) ? associatedColumnShellPage.ColumnParams.Column : 0;
				this.FindAscendant<ColumnsLayoutPage>()?.MoveFocusToPreviousBlade(currentBladeIndex);
				FileList.SelectedItem = null;
				ViewModel.ClearOpenedFolderSelectionIndicator();
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Right) // Right arrow: switch focus to next column
			{
				if (ViewModel.ParentShellPageInstance is not null && ViewModel.ParentShellPageInstance.ToolbarViewModel.IsEditModeEnabled)
					return;

				var currentBladeIndex = (ViewModel.ParentShellPageInstance is ColumnShellPage associatedColumnShellPage) ? associatedColumnShellPage.ColumnParams.Column : 0;
				this.FindAscendant<ColumnsLayoutPage>()?.MoveFocusToNextBlade(currentBladeIndex + 1);
				e.Handled = true;
			}
		}

		private async void MainUngroupedListView_ItemTapped(object sender, TappedRoutedEventArgs e)
		{
			var ctrlPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
			var shiftPressed = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
			var item = (e.OriginalSource as FrameworkElement)?.DataContext as ListedItem;


			// Allow for Ctrl+Shift selection
			if (ctrlPressed || shiftPressed)
				return;

			var isItemFile = item?.PrimaryItemAttribute is StorageItemTypes.File;
			var isItemFolder = item?.PrimaryItemAttribute is StorageItemTypes.Folder;

			// Check if the setting to open items with a single click is turned on
			if (UserSettingsService.FoldersSettingsService.OpenItemsWithOneClick && isItemFile)
			{
				ResetRenameDoubleClick();
				_ = NavigationHelpers.OpenSelectedItems(ParentShellPageInstance, false);
			}
			else
			{
				var clickedItem = e.OriginalSource as FrameworkElement;
				if (clickedItem is TextBlock textBlock && textBlock.Name == "ItemName")
				{
					CheckRenameDoubleClick(clickedItem.DataContext);
				}
				else if (IsRenamingItem &&
					FileList.ContainerFromItem(RenamingItem) is ListViewItem listViewItem &&
					listViewItem.FindDescendant("ListViewTextBoxItemName") is TextBox textBox)
				{
					await CommitRename(textBox);
				}

				if (isItemFolder && UserSettingsService.FoldersSettingsService.ColumnLayoutOpenFoldersWithOneClick)
				{
					ItemInvoked?.Invoke(
						new ColumnParam
						{
							Source = this,
							NavPathParam = (item is ShortcutItem sht ? sht.TargetPath : item!.ItemPath),
							ListView = FileList
						},
						EventArgs.Empty);
				}
				else if (!IsRenamingItem && (isItemFile || isItemFolder))
				{
					CheckDoubleClick(item!);
				}
			}
		}

		private void CloseFolder()
		{
			var currentBladeIndex = (ViewModel.ParentShellPageInstance is ColumnShellPage associatedColumnShellPage) ? associatedColumnShellPage.ColumnParams.Column : 0;

			this.FindAscendant<ColumnsLayoutPage>()?.DismissOtherBlades(currentBladeIndex);

			ViewModel.ClearOpenedFolderSelectionIndicator();
		}

		private void PageGotFocus(RoutedEventArgs e)
		{
			if (FileList.SelectedItem == null && openedFolderPresenter != null)
			{
				openedFolderPresenter.Focus(FocusState.Programmatic);
				FileList.SelectedItem = FileList.ItemFromContainer(openedFolderPresenter);
			}
		}

		private void ColumnViewBase_ItemInvoked(object? sender, EventArgs e)
		{
			ClearOpenedFolderSelectionIndicator();
			openedFolderPresenter = FileList.ContainerFromItem(FileList.SelectedItem) as ListViewItem;
		}

		private void HighlightPathDirectory(ListViewBase sender, ContainerContentChangingEventArgs args)
		{
			if (args.Item is ListedItem item && ParentColumnsLayoutPage?.OwnerPath is string ownerPath
				&& (ownerPath == item.ItemPath || ownerPath.StartsWith(item.ItemPath) && ownerPath[item.ItemPath.Length] is '/' or '\\'))
			{
				var presenter = args.ItemContainer.FindDescendant<Grid>()!;
				presenter!.Background = this.Resources["ListViewItemBackgroundSelected"] as SolidColorBrush;
				openedFolderPresenter = FileList.ContainerFromItem(item) as ListViewItem;
				FileList.ContainerContentChanging -= HighlightPathDirectory;
			}
		}

		private async Task ReloadItemIcons()
		{
			ParentShellPageInstance.FilesystemViewModel.CancelExtendedPropertiesLoading();
			foreach (ListedItem listedItem in ParentShellPageInstance.FilesystemViewModel.FilesAndFolders.ToList())
			{
				listedItem.ItemPropertiesInitialized = false;
				if (FileList.ContainerFromItem(listedItem) is not null)
					await ParentShellPageInstance.FilesystemViewModel.LoadExtendedItemProperties(listedItem, 24);
			}
		}

		private void CheckDoubleClick(ListedItem item)
		{
			doubleClickTimer.Debounce(() =>
			{
				ClearOpenedFolderSelectionIndicator();

				var itemPath = item!.ItemPath.EndsWith('\\')
					? item.ItemPath.Substring(0, item.ItemPath.Length - 1)
					: item.ItemPath;

				ItemTapped?.Invoke(new ColumnParam { Source = this, NavPathParam = Path.GetDirectoryName(itemPath), ListView = FileList }, EventArgs.Empty);

				doubleClickTimer.Stop();
			},
			TimeSpan.FromMilliseconds(200));
		}

		private void Grid_Loaded(object sender, RoutedEventArgs e)
		{
			var itemContainer = (sender as Grid)?.FindAscendant<ListViewItem>();

			if (itemContainer is null)
				return;

			itemContainer.ContextFlyout = ItemContextMenuFlyout;
		}

		internal void ClearSelectionIndicator()
		{
			LockPreviewPaneContent = true;
			FileList.SelectedItem = null;
			LockPreviewPaneContent = false;
		}
	}
}
