using CommunityToolkit.WinUI.UI;
using Files.App.Filesystem;
using Files.App.Helpers;
using Files.App.Helpers.XamlHelpers;
using Files.App.Interacts;
using Files.App.UserControls;
using Files.Shared.Enums;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.System;
using Windows.UI.Core;

namespace Files.App
{
	public abstract class StandardViewBase : BaseLayout
	{
		protected abstract ListViewBase ListViewBase
		{
			get;
		}

		protected override ItemsControl ItemsControl => ListViewBase;

		protected abstract SemanticZoom RootZoom
		{
			get;
		}

		public StandardViewBase() : base()
		{

		}

		protected override void InitializeCommandsViewModel()
		{
			CommandsViewModel = new BaseLayoutCommandsViewModel(new BaseLayoutCommandImplementationModel(ParentShellPageInstance, ItemManipulationModel));
		}

		protected override void HookEvents()
		{
			UnhookEvents();
			ItemManipulationModel.FocusFileListInvoked += ItemManipulationModel_FocusFileListInvoked;
			ItemManipulationModel.SelectAllItemsInvoked += ItemManipulationModel_SelectAllItemsInvoked;
			ItemManipulationModel.ClearSelectionInvoked += ItemManipulationModel_ClearSelectionInvoked;
			ItemManipulationModel.InvertSelectionInvoked += ItemManipulationModel_InvertSelectionInvoked;
			ItemManipulationModel.AddSelectedItemInvoked += ItemManipulationModel_AddSelectedItemInvoked;
			ItemManipulationModel.RemoveSelectedItemInvoked += ItemManipulationModel_RemoveSelectedItemInvoked;
			ItemManipulationModel.FocusSelectedItemsInvoked += ItemManipulationModel_FocusSelectedItemsInvoked;
			ItemManipulationModel.StartRenameItemInvoked += ItemManipulationModel_StartRenameItemInvoked;
			ItemManipulationModel.ScrollIntoViewInvoked += ItemManipulationModel_ScrollIntoViewInvoked;
		}

		protected override void UnhookEvents()
		{
			if (ItemManipulationModel is null)
				return;

			ItemManipulationModel.FocusFileListInvoked -= ItemManipulationModel_FocusFileListInvoked;
			ItemManipulationModel.SelectAllItemsInvoked -= ItemManipulationModel_SelectAllItemsInvoked;
			ItemManipulationModel.ClearSelectionInvoked -= ItemManipulationModel_ClearSelectionInvoked;
			ItemManipulationModel.InvertSelectionInvoked -= ItemManipulationModel_InvertSelectionInvoked;
			ItemManipulationModel.AddSelectedItemInvoked -= ItemManipulationModel_AddSelectedItemInvoked;
			ItemManipulationModel.RemoveSelectedItemInvoked -= ItemManipulationModel_RemoveSelectedItemInvoked;
			ItemManipulationModel.FocusSelectedItemsInvoked -= ItemManipulationModel_FocusSelectedItemsInvoked;
			ItemManipulationModel.StartRenameItemInvoked -= ItemManipulationModel_StartRenameItemInvoked;
			ItemManipulationModel.ScrollIntoViewInvoked -= ItemManipulationModel_ScrollIntoViewInvoked;
		}

		protected virtual void ItemManipulationModel_FocusFileListInvoked(object? sender, EventArgs e)
		{
			var focusedElement = (FrameworkElement)FocusManager.GetFocusedElement(XamlRoot);
			var isFileListFocused = DependencyObjectHelpers.FindParent<ListViewBase>(focusedElement) == ItemsControl;
			if (!isFileListFocused)
				ListViewBase.Focus(FocusState.Programmatic);
		}

		protected virtual void ItemManipulationModel_SelectAllItemsInvoked(object? sender, EventArgs e)
		{
			ListViewBase.SelectAll();
		}

		protected virtual void ItemManipulationModel_ClearSelectionInvoked(object? sender, EventArgs e)
		{
			ListViewBase.SelectedItems.Clear();
		}

		protected virtual void ItemManipulationModel_InvertSelectionInvoked(object? sender, EventArgs e)
		{
			if (SelectedItems.Count < GetAllItems().Count() / 2)
			{
				var oldSelectedItems = SelectedItems.ToList();
				ItemManipulationModel.SelectAllItems();
				ItemManipulationModel.RemoveSelectedItems(oldSelectedItems);
				return;
			}

			List<ListedItem> newSelectedItems = GetAllItems()
				.Cast<ListedItem>()
				.Except(SelectedItems)
				.ToList();

			ItemManipulationModel.SetSelectedItems(newSelectedItems);
		}

		protected virtual void ItemManipulationModel_StartRenameItemInvoked(object? sender, EventArgs e)
		{
			StartRenameItem();
		}

		protected abstract void ItemManipulationModel_AddSelectedItemInvoked(object? sender, ListedItem e);

		protected abstract void ItemManipulationModel_RemoveSelectedItemInvoked(object? sender, ListedItem e);

		protected abstract void ItemManipulationModel_FocusSelectedItemsInvoked(object? sender, EventArgs e);

		protected abstract void ItemManipulationModel_ScrollIntoViewInvoked(object? sender, ListedItem e);

		protected virtual void ZoomIn(object? sender, GroupOption option)
		{
			if (option == GroupOption.None)
				RootZoom.IsZoomedInViewActive = true;
		}

		protected virtual async void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectedItems = ListViewBase.SelectedItems.Cast<ListedItem>().Where(x => x is not null).ToList();
			if (SelectedItems.Count == 1 && App.AppModel.IsQuickLookAvailable)
				await QuickLookHelpers.ToggleQuickLook(ParentShellPageInstance, true);
		}

		protected abstract void FileList_PreviewKeyDown(object sender, KeyRoutedEventArgs e);

		protected virtual void SelectionRectangle_SelectionEnded(object? sender, EventArgs e)
		{
			ListViewBase.Focus(FocusState.Programmatic);
		}

		protected virtual void StartRenameItem(string itemNameTextBox)
		{
			RenamingItem = SelectedItem;
			if (RenamingItem is null)
				return;

			int extensionLength = RenamingItem.FileExtension?.Length ?? 0;

			ListViewItem? listViewItem = ListViewBase.ContainerFromItem(RenamingItem) as ListViewItem;
			if (listViewItem is null)
				return;

			TextBox? textBox = null;
			TextBlock? textBlock = listViewItem.FindDescendant("ItemName") as TextBlock;
			textBox = listViewItem.FindDescendant(itemNameTextBox) as TextBox;
			textBox!.Text = textBlock!.Text;
			OldItemName = textBlock.Text;
			textBlock.Visibility = Visibility.Collapsed;
			textBox.Visibility = Visibility.Visible;
			Grid.SetColumnSpan(textBox.FindParent<Grid>(), 8);

			textBox.Focus(FocusState.Pointer);
			textBox.LostFocus += RenameTextBox_LostFocus;
			textBox.KeyDown += RenameTextBox_KeyDown;

			int selectedTextLength = SelectedItem.Name.Length;
			if (!SelectedItem.IsShortcut && UserSettingsService.FoldersSettingsService.ShowFileExtensions)
				selectedTextLength -= extensionLength;
			textBox.Select(0, selectedTextLength);
			IsRenamingItem = true;
		}

		protected abstract void EndRename(TextBox textBox);

		protected virtual async void CommitRename(TextBox textBox)
		{
			EndRename(textBox);
			string newItemName = textBox.Text.Trim().TrimEnd('.');
			await UIFilesystemHelpers.RenameFileItemAsync(RenamingItem, newItemName, ParentShellPageInstance);
		}

		protected virtual void RenameTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			// This check allows the user to use the text box context menu without ending the rename
			if (!(FocusManager.GetFocusedElement(XamlRoot) is AppBarButton or Popup))
			{
				TextBox textBox = (TextBox)e.OriginalSource;
				CommitRename(textBox);
			}
		}

		protected void RenameTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
		{
			if (e.Key == VirtualKey.Escape)
			{
				TextBox textBox = (TextBox)sender;
				textBox.LostFocus -= RenameTextBox_LostFocus;
				textBox.Text = OldItemName;
				EndRename(textBox);
				e.Handled = true;
			}
			else if (e.Key == VirtualKey.Enter)
			{
				TextBox textBox = (TextBox)sender;
				textBox.LostFocus -= RenameTextBox_LostFocus;
				CommitRename(textBox);
				e.Handled = true;
			}
		}

		protected override void Page_CharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
		{
			if (ParentShellPageInstance is null)
				return;

			if (ParentShellPageInstance.CurrentPageType != this.GetType() || IsRenamingItem)
				return;

			// Don't block the various uses of enter key (key 13)
			var focusedElement = (FrameworkElement)FocusManager.GetFocusedElement(XamlRoot);
			var isHeaderFocused = DependencyObjectHelpers.FindParent<DataGridHeader>(focusedElement) is not null;
			if (InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Enter) == CoreVirtualKeyStates.Down
				|| (focusedElement is Button && !isHeaderFocused) // Allow jumpstring when header is focused
				|| focusedElement is TextBox
				|| focusedElement is PasswordBox
				|| DependencyObjectHelpers.FindParent<ContentDialog>(focusedElement) is not null)
				return;

			base.Page_CharacterReceived(sender, args);
		}

		public override void Dispose()
		{
			base.Dispose();
			UnhookEvents();
			CommandsViewModel?.Dispose();
		}
	}
}
