// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Files.App.Dialogs;
using Files.App.ViewModels.Settings;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using WinRT;

namespace Files.App.Views.Settings
{
	/// <summary>
	/// Represents settings page called Actions, which provides a way to customize key bindings.
	/// </summary>
	public sealed partial class ActionsPage : Page
	{
		private readonly string PART_RowActions = "RowActions";

		private ModifiableActionItem? _itemPendingDeletion;

		private ActionsViewModel ViewModel { get; set; } = new();

		public ActionsPage()
		{
			InitializeComponent();
		}

		private void ActionsPage_Loaded(object sender, RoutedEventArgs e)
		{
			if (ViewModel.LoadAllActionsCommand.CanExecute(e))
				ViewModel.LoadAllActionsCommand.Execute(e);
		}

		[DynamicWindowsRuntimeCast(typeof(UserControl))]
		private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			// Reveal the edit and delete buttons on pointer in
			if (sender is UserControl userControl && userControl.FindChild(PART_RowActions) is FrameworkElement rowActions)
				rowActions.Visibility = Visibility.Visible;
		}

		[DynamicWindowsRuntimeCast(typeof(UserControl))]
		private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			// Hide the edit and delete buttons on pointer out
			if (sender is UserControl userControl && userControl.FindChild(PART_RowActions) is FrameworkElement rowActions)
				rowActions.Visibility = Visibility.Collapsed;
		}

		private async void AddCommandButton_Click(SplitButton sender, SplitButtonClickEventArgs e)
		{
			await ShowKeyBindingEditorDialogAsync(null);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private async void EditButton_Click(object sender, RoutedEventArgs e)
		{
			// ItemsRepeater doesn't set DataContext on realized rows; the item comes through Tag
			if (sender is FrameworkElement { Tag: ModifiableActionItem item })
				await ShowKeyBindingEditorDialogAsync(item);
		}

		[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
		private void DeleteButton_Click(object sender, RoutedEventArgs e)
		{
			// ItemsRepeater doesn't set DataContext on realized rows; the item comes through Tag
			if (sender is not FrameworkElement { Tag: ModifiableActionItem item } target)
				return;

			// Confirm before removing, anchored to the delete button
			_itemPendingDeletion = item;
			DeleteConfirmationTeachingTip.Target = target;
			DeleteConfirmationTeachingTip.IsOpen = true;
		}

		private void DeleteConfirmationTeachingTip_ActionButtonClick(TeachingTip sender, object args)
		{
			sender.IsOpen = false;

			if (_itemPendingDeletion is not null)
			{
				ViewModel.DeleteCommand.Execute(_itemPendingDeletion);
				_itemPendingDeletion = null;
			}
		}

		private async Task ShowKeyBindingEditorDialogAsync(ModifiableActionItem? itemToEdit)
		{
			var dialog = new KeyBindingEditorDialog(ViewModel, itemToEdit)
			{
				XamlRoot = XamlRoot,
			};

			await dialog.ShowAsync();
		}

		[DynamicWindowsRuntimeCast(typeof(TextBox))]
		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			var query = ((TextBox)sender).Text;
			ViewModel.FilterItems(query);
		}
	}
}
