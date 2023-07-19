// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using CommunityToolkit.WinUI.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.IO;
using System.Windows.Input;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;

namespace Files.App.ViewModels.ContentLayouts
{
	public class ColumnsLayoutBaseViewModel : GroupableLayoutViewModel
	{
		protected override uint IconSize
			=> Constants.Browser.ColumnViewBrowser.ColumnViewSizeSmall;

		private readonly DispatcherQueueTimer doubleClickTimer;

		public ColumnsLayoutPage? ParentColumnsLayoutPage { get; private set; }

		private ListViewItem? openedFolderPresenter;

		public event EventHandler? ItemInvoked;
		public event EventHandler? ItemTapped;

		public ICommand PageGotFocusCommand;

		public ColumnsLayoutBaseViewModel() : base()
		{
			PageGotFocusCommand = new RelayCommand<RoutedEventArgs>(PageGotFocus);

			ItemInvoked += ColumnViewBase_ItemInvoked;
			GotFocus += PageGotFocus;

			doubleClickTimer = MainWindow.Instance.DispatcherQueue.CreateTimer();
		}

		#region protected overrides
		public override void OnNavigatedTo(NavigationEventArgs eventArgs)
		{
			if (eventArgs.Parameter is NavigationArguments navArgs)
			{
				ParentColumnsLayoutPage = (navArgs.AssociatedTabInstance as FrameworkElement)?.FindAscendant<ColumnsLayoutPage>();

				var index = (navArgs.AssociatedTabInstance as ColumnShellPage)?.ColumnParams?.Column;

				navArgs.FocusOnNavigation = index == ParentColumnsLayoutPage?.FocusIndex;

				if (index < ParentColumnsLayoutPage?.FocusIndex)
					FileList.ContainerContentChanging += HighlightPathDirectory;
			}

			base.OnNavigatedTo(eventArgs);

			FolderSettings.GroupOptionPreferenceUpdated -= ZoomIn;
			FolderSettings.GroupOptionPreferenceUpdated += ZoomIn;
		}

		public override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
		}

		protected override void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e)
		{
			try
			{
				FileList.ScrollIntoView(e, ScrollIntoViewAlignment.Default);
			}
			catch (Exception)
			{
				// Catch error where row index could not be found
			}
		}

		protected override void ItemManipulationModel_FocusSelectedItemsInvoked(object? sender, EventArgs e)
		{
			if (SelectedItems.Any())
			{
				FileList.ScrollIntoView(SelectedItems.Last());
				(FileList.ContainerFromItem(SelectedItems.Last()) as ListViewItem)?.Focus(FocusState.Keyboard);
			}
		}

		protected override void ItemManipulationModel_AddSelectedItemInvoked(object? sender, ListedItem e)
		{
			if (NextRenameIndex != 0 && TryStartRenameNextItem(e))
				return;

			FileList?.SelectedItems.Add(e);
		}

		protected override void ItemManipulationModel_RemoveSelectedItemInvoked(object? sender, ListedItem e)
		{
			FileList?.SelectedItems.Remove(e);
		}

		protected override void EndRename(TextBox textBox)
		{
			if (textBox is not null && textBox.Parent is not null)
			{
				// Re-focus selected list item
				var listViewItem = FileList.ContainerFromItem(RenamingItem) as ListViewItem;
				listViewItem?.Focus(FocusState.Programmatic);

				var textBlock = listViewItem?.FindDescendant("ItemName") as TextBlock;
				textBox!.Visibility = Visibility.Collapsed;
				textBlock!.Visibility = Visibility.Visible;
			}

			textBox!.LostFocus -= RenameTextBox_LostFocus;
			textBox.KeyDown -= RenameTextBox_KeyDown;
			FileNameTeachingTip.IsOpen = false;
			IsRenamingItem = false;
		}

		protected override void BaseFolderSettings_LayoutModeChangeRequested(object? sender, LayoutModeEventArgs e)
		{
			var parent = this.FindAscendant<ModernShellPage>();

			if (parent is null)
				return;

			switch (e.LayoutMode)
			{
				case FolderLayoutModes.ColumnView:
					break;
				case FolderLayoutModes.DetailsView:
					parent.FolderSettings.ToggleLayoutModeDetailsView(true);
					break;
				case FolderLayoutModes.TilesView:
					parent.FolderSettings.ToggleLayoutModeTiles(true);
					break;
				case FolderLayoutModes.GridView:
					parent.FolderSettings.ToggleLayoutModeGridView(e.GridViewSize);
					break;
				case FolderLayoutModes.Adaptive:
					parent.FolderSettings.ToggleLayoutModeAdaptive();
					break;
			}
		}

		protected override bool CanGetItemFromElement(object element)
		{
			return element is ListViewItem;
		}
		#endregion

		public override void StartRenameItem()
		{
			StartRenameItem("ListViewTextBoxItemName");
		}

		public void ClearOpenedFolderSelectionIndicator()
		{
			if (openedFolderPresenter is null)
				return;

			openedFolderPresenter.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
			var presenter = openedFolderPresenter.FindDescendant<Grid>()!;
			presenter!.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
			openedFolderPresenter = null;
		}

		public override void ResetItemOpacity()
		{
		}

		public void HandleRightClick(object sender, RightTappedRoutedEventArgs e)
		{
			HandleRightClick(e.OriginalSource);
		}

		public void HandleRightClick(object sender, HoldingRoutedEventArgs e)
		{
			HandleRightClick(e.OriginalSource);
		}

		public void HandleRightClick(object pressed)
		{
			var objectPressed = ((FrameworkElement)pressed).DataContext as ListedItem;

			// Check if RightTapped row is currently selected
			if (objectPressed is not null || (IsItemSelected && SelectedItems.Contains(objectPressed)))
				return;

			// The following code is only reachable when a user RightTapped an unselected row
			ItemManipulationModel.SetSelectedItem(objectPressed);
		}

		public override void Dispose()
		{
			base.Dispose();

			ParentColumnsLayoutPage = null;
		}
	}
}
